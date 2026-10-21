import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBookings, getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import {
    clockMinutes, formatDay, formatTime, isIsoDate, osloMinutes, shiftDay, todayInOslo, weekdayOf,
} from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import type { BusinessDetails, PanelBooking } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { cancelBookingAction, confirmBookingAction } from "./actions";
import NewBookingForm from "./NewBookingForm";
import PanelNav from "./PanelNav";
import styles from "./page.module.css";
import calendar from "./calendar.module.css";

// One hour in the grid is 80 px high (a 30 min visit = 40 px: time and customer fit)
const PX_PER_MINUTE = 80 / 60;

type CalendarPageProps = {
    searchParams: Promise<{
        dag?: string; visning?: string; ansatt?: string; avlyste?: string; bestilling?: string; feil?: string;
        ny?: string; tid?: string; for?: string;
    }>;
};

type Column = { id: string; title: string; bookings: PanelBooking[] };
type Range = { start: number; end: number };

// The business calendar: one day with a column per employee, or one week with a column per day
export default async function CalendarPage({ searchParams }: CalendarPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const query = await searchParams;
    const day = isIsoDate(query.dag) ? query.dag : todayInOslo();
    const week = query.visning === "uke";
    const showCancelled = query.avlyste === "1";
    // Week = Monday to Sunday around the chosen day
    const first = week ? shiftDay(day, -((weekdayOf(day) + 6) % 7)) : day;
    const last = week ? shiftDay(first, 6) : day;

    const [business, allBookings] = await Promise.all([getPanelBusiness(token), getPanelBookings(token, first, last)]);
    if (!business || !allBookings) {
        redirect("/logg-inn");
    }

    const employeeFilter = week && business.employees.some((e) => e.id === query.ansatt) ? query.ansatt : undefined;
    const bookings = allBookings
        .filter((b) => showCancelled || b.status !== "Avlyst")
        .filter((b) => !employeeFilter || b.employeeId === employeeFilter);
    const columns = week ? dayColumns(first, bookings) : employeeColumns(business, bookings);
    const range = visibleRange(business, week ? weekDays(first) : [day], bookings);

    // The same view with one value changed (keeps the other choices)
    const link = (change: Record<string, string | undefined>) => {
        const params = new URLSearchParams();
        const values = { dag: day, visning: week ? "uke" : undefined, ansatt: employeeFilter,
            avlyste: showCancelled ? "1" : undefined, ...change };
        for (const [key, value] of Object.entries(values)) {
            if (value) params.set(key, value);
        }
        return `/panel?${params}`;
    };
    const step = week ? 7 : 1;
    // The visit opened in the details window (also a cancelled one, even when those are hidden)
    const selected = allBookings.find((b) => b.id === query.bestilling);
    const here = link({}); // this view without the details window
    // "+ Ny bestilling", or a click on an empty place in the grid (time and employee filled in)
    const newBooking = query.ny === "1";
    const newTime = query.tid && /^\d{2}:\d{2}$/.test(query.tid) ? query.tid : "09:00";
    const newEmployee = business.employees.some((e) => e.id === query.for) ? query.for : undefined;

    return (
        <main className={`${styles.main} ${calendar.wide}`}>
            <PanelNav title="Kalender"/>

            {query.feil && (
                <p className={styles.error} role="alert">
                    {query.feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}

            <div className={calendar.toolbar}>
                <div className={calendar.segmented} role="group" aria-label="Visning">
                    <Link href={link({ visning: undefined, ansatt: undefined })} aria-current={week ? undefined : "page"}>Dag</Link>
                    <Link href={link({ visning: "uke" })} aria-current={week ? "page" : undefined}>Uke</Link>
                </div>
                <Link href={link({ ny: "1" })} scroll={false} className={styles.primaryButton}>+ Ny bestilling</Link>
                <Link href={link({ dag: shiftDay(day, -step) })} className={styles.secondaryButton}
                      aria-label={week ? "Forrige uke" : "Forrige dag"}>←</Link>
                <Link href={link({ dag: todayInOslo() })} className={styles.secondaryButton}>I dag</Link>
                <Link href={link({ dag: shiftDay(day, step) })} className={styles.secondaryButton}
                      aria-label={week ? "Neste uke" : "Neste dag"}>→</Link>
                <h2 className={calendar.dayTitle}>
                    {week ? `Uke ${isoWeek(first)} · ${shortDay(first)} – ${shortDay(last)}` : formatDay(day)}
                </h2>
                <form method="get" className={calendar.datePicker}>
                    {week && <input type="hidden" name="visning" value="uke"/>}
                    {employeeFilter && <input type="hidden" name="ansatt" value={employeeFilter}/>}
                    {showCancelled && <input type="hidden" name="avlyste" value="1"/>}
                    <label>
                        <span className={calendar.srOnly}>Velg dato</span>
                        <input type="date" name="dag" defaultValue={day}/>
                    </label>
                    <button type="submit" className={styles.secondaryButton}>Vis</button>
                </form>
            </div>

            <div className={calendar.filters}>
                {week && (
                    <form method="get" className={calendar.employeeFilter}>
                        <input type="hidden" name="visning" value="uke"/>
                        <input type="hidden" name="dag" value={day}/>
                        {showCancelled && <input type="hidden" name="avlyste" value="1"/>}
                        <label>
                            Ansatt{" "}
                            <select name="ansatt" defaultValue={employeeFilter ?? ""}>
                                <option value="">Alle ansatte</option>
                                {business.employees.map((e) => (
                                    <option key={e.id} value={e.id}>{e.name} {e.lastName}</option>
                                ))}
                            </select>
                        </label>
                        <button type="submit" className={styles.secondaryButton}>Vis</button>
                    </form>
                )}
                <Link href={link({ avlyste: showCancelled ? undefined : "1" })} className={calendar.toggle}>
                    {showCancelled ? "Skjul avlyste" : "Vis avlyste"}
                </Link>
            </div>

            {!week && !business.openingHours.some((h) => h.dayOfWeek === weekdayOf(day)) && (
                <p className={styles.muted}>Stengt denne dagen.</p>
            )}

            <TimeGrid columns={columns} range={range} showEmployee={week}
                      hrefFor={(booking) => link({ bestilling: booking.id })}
                      newHref={(column, minute) => link({
                          ny: "1",
                          tid: clock(minute),
                          // day view: the column is an employee; week view: the column is a day
                          ...(week ? { dag: column.id } : { for: column.id }),
                      })}/>

            {/* Phone: the same visits as a simple list (the grid is too narrow there) */}
            <PhoneList columns={week ? columns : [{ id: day, title: formatDay(day), bookings }]}
                       hrefFor={(booking) => link({ bestilling: booking.id })}/>

            {selected && <BookingDetails booking={selected} closeHref={here}/>}
            {newBooking && (
                <NewBookingForm services={business.services} employees={business.employees} date={day}
                                time={newTime} employeeId={newEmployee ?? employeeFilter} week={week} closeHref={here}/>
            )}
        </main>
    );
}

// The grid: hours on the left, one column per employee (day) or per day (week)
type Href = (booking: PanelBooking) => string;

function TimeGrid({ columns, range, showEmployee, hrefFor, newHref }: {
    columns: Column[]; range: Range; showEmployee: boolean; hrefFor: Href;
    newHref: (column: Column, minute: number) => string;
}) {
    const height = (range.end - range.start) * PX_PER_MINUTE;
    const marks = hourMarks(range);
    return (
        <div className={calendar.scroller}>
            <div className={calendar.grid} style={{ gridTemplateColumns: `56px repeat(${columns.length}, minmax(140px, 1fr))` }}>
                <div className={calendar.corner}/>
                {columns.map((column) => (
                    <div key={column.id} className={calendar.columnHead}>{column.title}</div>
                ))}

                <div className={calendar.hours} style={{ height }}>
                    {marks.map((minute) => (
                        <span key={minute} style={{ top: (minute - range.start) * PX_PER_MINUTE }}>
                            {String(minute / 60).padStart(2, "0")}:00
                        </span>
                    ))}
                </div>

                {columns.map((column) => (
                    <div key={column.id} className={calendar.column} style={{ height }}>
                        {marks.map((minute) => (
                            <div key={minute} className={calendar.hourLine} style={{ top: (minute - range.start) * PX_PER_MINUTE }}/>
                        ))}
                        {/* Empty half hours: a click opens "Ny bestilling" with this time (visits lie on top) */}
                        {halfHours(range).map((minute) => (
                            <Link key={minute} href={newHref(column, minute)} scroll={false} className={calendar.emptySlot}
                                  style={{ top: (minute - range.start) * PX_PER_MINUTE, height: 30 * PX_PER_MINUTE }}
                                  aria-label={`Ny bestilling ${column.title} kl. ${clock(minute)}`}/>
                        ))}
                        {column.bookings.map((booking) => {
                            const start = Math.max(osloMinutes(booking.startUtc), range.start);
                            const end = Math.min(osloMinutes(booking.endUtc) || 24 * 60, range.end);
                            return (
                                <Link
                                    key={booking.id}
                                    href={hrefFor(booking)}
                                    scroll={false}
                                    className={`${calendar.booking} ${calendar[booking.status] ?? ""}`}
                                    style={{ top: (start - range.start) * PX_PER_MINUTE, height: Math.max(end - start, 15) * PX_PER_MINUTE }}
                                    title={`${booking.customerName} – ${booking.serviceName} hos ${booking.employeeName} (${statusLabels[booking.status] ?? booking.status})`}
                                >
                                    <span className={calendar.time}>{formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}</span>
                                    <span className={calendar.customer}>{booking.customerName}</span>
                                    <span className={calendar.service}>
                                        {showEmployee ? `${booking.employeeName} · ${booking.serviceName}` : booking.serviceName}
                                    </span>
                                </Link>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
}

// Phone: the visits of each day as a list of cards; a tap opens the details window
function PhoneList({ columns, hrefFor }: { columns: Column[]; hrefFor: Href }) {
    const days = columns.filter((c) => c.bookings.length > 0);
    return (
        <div className={calendar.phoneList}>
            {days.length === 0 && <p className={styles.muted}>Ingen bestillinger.</p>}
            {days.map((column) => (
                <section key={column.id}>
                    <h3 className={calendar.phoneDay}>{column.title}</h3>
                    <ul className={styles.list}>
                        {[...column.bookings].sort((a, b) => a.startUtc.localeCompare(b.startUtc)).map((booking) => (
                            <li key={booking.id}>
                                <Link href={hrefFor(booking)} scroll={false}
                                      className={`${calendar.phoneCard} ${calendar[booking.status] ?? ""}`}>
                                    <span className={calendar.time}>{formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}</span>
                                    <span>{booking.customerName}</span>
                                    <span className={calendar.service}>{booking.serviceName} hos {booking.employeeName}</span>
                                </Link>
                            </li>
                        ))}
                    </ul>
                </section>
            ))}
        </div>
    );
}

// The details window for one visit: customer, contact, note and Bekreft / Avlys
function BookingDetails({ booking, closeHref }: { booking: PanelBooking; closeHref: string }) {
    const { canConfirm, canCancel } = allowedActions(booking);
    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="booking-title">
                <div className={calendar.dialogHead}>
                    <h2 id="booking-title" className={calendar.dialogTitle}>{booking.customerName}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogWhen}>
                    {formatDay(osloDate(booking.startUtc))}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                </p>
                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
                    {statusLabels[booking.status] ?? booking.status}
                </span>
                <p className={styles.service}>{booking.serviceName} hos {booking.employeeName}</p>
                <p className={styles.customer}>
                    <a href={`tel:${booking.customerPhone}`}>{booking.customerPhone}</a>
                    {booking.customerEmail && (
                        <>
                            {" · "}
                            <a href={`mailto:${booking.customerEmail}`}>{booking.customerEmail}</a>
                        </>
                    )}
                </p>
                {booking.customerNote && (
                    <div className={styles.note}>
                        <p><span aria-hidden="true">💬</span> {booking.customerNote}</p>
                        {booking.customerEmail && (
                            <a href={`mailto:${booking.customerEmail}?subject=${encodeURIComponent("Svar på bestillingen din")}`}>
                                Svar på e-post
                            </a>
                        )}
                    </div>
                )}
                {(canConfirm || canCancel) && (
                    <div className={styles.actions}>
                        {canConfirm && (
                            <form action={confirmBookingAction}>
                                <input type="hidden" name="bookingId" value={booking.id}/>
                                <input type="hidden" name="tilbake" value={closeHref}/>
                                <SubmitButton className={styles.primaryButton} pendingText="Bekrefter…">Bekreft</SubmitButton>
                            </form>
                        )}
                        {canCancel && (
                            <details>
                                <summary className={styles.dangerButton}>Avlys</summary>
                                <p>Er du sikker?{booking.customerEmail && " Kunden får beskjed på e-post."}</p>
                                <form action={cancelBookingAction}>
                                    <input type="hidden" name="bookingId" value={booking.id}/>
                                    <input type="hidden" name="tilbake" value={closeHref}/>
                                    <SubmitButton className={styles.dangerButton} pendingText="Avlyser…">Ja, avlys</SubmitButton>
                                </form>
                            </details>
                        )}
                    </div>
                )}
            </section>
        </div>
    );
}

// Bekreft while waiting; Avlys only before the visit starts (the API refuses it afterwards)
function allowedActions(booking: PanelBooking) {
    const started = new Date(booking.startUtc).getTime() <= Date.now();
    return {
        canConfirm: booking.status === "Venter",
        canCancel: booking.status !== "Avlyst" && !started,
    };
}

// Day view: the employees as columns; a booking for an employee who was removed still gets a column
function employeeColumns(business: BusinessDetails, bookings: PanelBooking[]): Column[] {
    const columns: Column[] = business.employees.map((e) => ({
        id: e.id,
        title: `${e.name} ${e.lastName}`.trim(),
        bookings: bookings.filter((b) => b.employeeId === e.id),
    }));
    for (const booking of bookings) {
        if (!columns.some((c) => c.id === booking.employeeId)) {
            columns.push({
                id: booking.employeeId,
                title: booking.employeeName,
                bookings: bookings.filter((b) => b.employeeId === booking.employeeId),
            });
        }
    }
    return columns;
}

// Week view: Monday to Sunday as columns
function dayColumns(monday: string, bookings: PanelBooking[]): Column[] {
    return weekDays(monday).map((date) => ({
        id: date,
        title: shortDay(date),
        bookings: bookings.filter((b) => osloDate(b.startUtc) === date),
    }));
}

// "2026-10-19" -> "man. 19.10." (short enough for a column head)
function shortDay(date: string): string {
    return new Intl.DateTimeFormat("nb-NO", { timeZone: "UTC", weekday: "short", day: "numeric", month: "numeric" })
        .format(new Date(`${date}T12:00:00Z`));
}

function weekDays(monday: string): string[] {
    return [0, 1, 2, 3, 4, 5, 6].map((n) => shiftDay(monday, n));
}

// "2026-10-26T08:30:00Z" -> "2026-10-26" (the day in Oslo)
function osloDate(utc: string): string {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Oslo" }).format(new Date(utc));
}

// From the earliest opening to the latest closing of the shown days, widened to whole hours and to visits
function visibleRange(business: BusinessDetails, days: string[], bookings: PanelBooking[]): Range {
    const hours = business.openingHours.filter((h) => days.some((d) => weekdayOf(d) === h.dayOfWeek));
    let start = hours.length ? Math.min(...hours.map((h) => clockMinutes(h.opensAt))) : 8 * 60;
    let end = hours.length ? Math.max(...hours.map((h) => clockMinutes(h.closesAt))) : 17 * 60;
    for (const booking of bookings) {
        start = Math.min(start, osloMinutes(booking.startUtc));
        end = Math.max(end, osloMinutes(booking.endUtc) || 24 * 60);
    }
    return { start: Math.floor(start / 60) * 60, end: Math.min(Math.ceil(end / 60) * 60, 24 * 60) };
}

// 480, 540, 600 ... for every whole hour in the range
function hourMarks(range: Range): number[] {
    const marks: number[] = [];
    for (let minute = range.start; minute < range.end; minute += 60) {
        marks.push(minute);
    }
    return marks;
}

// Every half hour in the range: 480, 510, 540 ...
function halfHours(range: Range): number[] {
    const slots: number[] = [];
    for (let minute = range.start; minute < range.end; minute += 30) {
        slots.push(minute);
    }
    return slots;
}

// 570 -> "09:30"
function clock(minute: number): string {
    return `${String(Math.floor(minute / 60)).padStart(2, "0")}:${String(minute % 60).padStart(2, "0")}`;
}

// ISO week number (week 1 has the first Thursday of the year), as on Norwegian calendars
function isoWeek(monday: string): number {
    const thursday = new Date(`${shiftDay(monday, 3)}T12:00:00Z`);
    const yearStart = new Date(Date.UTC(thursday.getUTCFullYear(), 0, 1));
    return Math.ceil(((thursday.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
}
