import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBookings, getPanelBusiness, getPanelTimeOff } from "@/lib/api";
import { getToken } from "@/lib/auth";
import {
    clockMinutes, formatDay, formatTime, isIsoDate, osloMinutes, shiftDay, todayInOslo, weekdayOf,
} from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import type { BusinessDetails, PanelBooking, TimeOff } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { cancelBookingAction, confirmBookingAction, deleteTimeOffAction } from "./actions";
import MoveBookingForm from "./MoveBookingForm";
import NewBookingForm from "./NewBookingForm";
import PanelNav from "./PanelNav";
import PendingBadge from "./PendingBadge";
import TimeOffForm from "./TimeOffForm";
import styles from "./page.module.css";
import calendar from "./calendar.module.css";

// One hour in the grid is 120 px high (a 30 min visit = 60 px: time, customer and service fit)
const PX_PER_MINUTE = 2;

type CalendarPageProps = {
    searchParams: Promise<{
        dag?: string; visning?: string; ansatt?: string; bestilling?: string; flytt?: string; feil?: string;
        ny?: string; tid?: string; for?: string; blokker?: string; blokkering?: string; varsel?: string;
    }>;
};

// day = the date shown in the column (the same for every employee in the day view)
type Column = { id: string; title: string; day: string; bookings: PanelBooking[]; blocks: TimeOff[] };
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
    const month = query.visning === "maned";
    const view = week ? "uke" : month ? "maned" : undefined;
    // Week = Monday to Sunday around the chosen day; month = whole weeks from the 1st to the last day
    const monthStart = `${day.slice(0, 7)}-01`;
    const first = week ? mondayOf(day) : month ? mondayOf(monthStart) : day;
    const last = week ? shiftDay(first, 6) : month ? shiftDay(mondayOf(lastDayOfMonth(day)), 6) : day;

    const [business, allBookings, allBlocks] = await Promise.all([
        getPanelBusiness(token), getPanelBookings(token, first, last), getPanelTimeOff(token, first, last),
    ]);
    if (!business || !allBookings || !allBlocks) {
        redirect("/logg-inn");
    }

    const employeeFilter = (week || month) && business.employees.some((e) => e.id === query.ansatt) ? query.ansatt : undefined;
    const bookings = allBookings
        // Cancelled visits are gone from the calendar (a link from a notification can still open one)
        .filter((b) => b.status !== "Avlyst")
        .filter((b) => !employeeFilter || b.employeeId === employeeFilter);
    // Week or month view with one employee: that employee's blocks and the closed days of the whole business
    const blocks = allBlocks.filter((t) => !employeeFilter || !t.employeeId || t.employeeId === employeeFilter);
    const columns = view ? dayColumns(first, last, bookings, blocks) : employeeColumns(business, bookings, blocks, day);
    const range = visibleRange(business, week ? weekDays(first) : [day], bookings);

    // The same view with one value changed (keeps the other choices)
    const link = (change: Record<string, string | undefined>) => {
        const params = new URLSearchParams();
        const values = { dag: day, visning: view, ansatt: employeeFilter, ...change };
        for (const [key, value] of Object.entries(values)) {
            if (value) params.set(key, value);
        }
        return `/panel?${params}`;
    };
    const step = week ? 7 : 1;
    const previous = month ? shiftMonth(day, -1) : shiftDay(day, -step);
    const next = month ? shiftMonth(day, 1) : shiftDay(day, step);
    const unit = month ? "måned" : week ? "uke" : "dag";
    // The visit opened in the details window (also a cancelled one, even when those are hidden)
    const selected = allBookings.find((b) => b.id === query.bestilling);
    const moving = allBookings.find((b) => b.id === query.flytt && allowedActions(b).canMove);
    const here = link({}); // this view without the details window
    // "+ Ny bestilling", or a click on an empty place in the grid (time and employee filled in)
    const newBooking = query.ny === "1";
    const newTime = query.tid && /^\d{2}:\d{2}$/.test(query.tid) ? query.tid : "09:00";
    const newEmployee = business.employees.some((e) => e.id === query.for) ? query.for : undefined;
    const newTimeOff = query.blokker === "1";
    const selectedBlock = allBlocks.find((t) => t.id === query.blokkering);
    // Visits already in a time that was just blocked (they are kept)
    const overlapping = Number(query.varsel);

    return (
        <main className={`${styles.main} ${calendar.wide}`}>
            <PanelNav title="Kalender"/>
            {business.isHidden && (
                <p className={calendar.notice} role="status">
                    Bedriften venter på godkjenning – vanligvis innen én arbeidsdag. Du kan allerede legge inn
                    tjenester, ansatte og åpningstider. Kundene ser bedriften når den er godkjent.
                </p>
            )}
            {query.feil && (
                <p className={styles.error} role="alert">
                    {query.feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}
            {Number.isInteger(overlapping) && overlapping > 0 && (
                <p className={calendar.notice} role="status">
                    Tiden er blokkert, men det finnes allerede {overlapping === 1 ? "1 bestilling" : `${overlapping} bestillinger`} i
                    perioden. De er ikke avlyst – åpne dem i kalenderen hvis kunden må få beskjed.
                </p>
            )}

            <div className={calendar.toolbar}>
                <div className={calendar.segmented} role="group" aria-label="Visning">
                    <Link href={link({ visning: undefined, ansatt: undefined })} aria-current={view ? undefined : "page"}>Dag</Link>
                    <Link href={link({ visning: "uke" })} aria-current={week ? "page" : undefined}>Uke</Link>
                    <Link href={link({ visning: "maned" })} aria-current={month ? "page" : undefined}>Måned</Link>
                </div>
                <Link href={link({ ny: "1" })} scroll={false} className={styles.primaryButton}>+ Ny bestilling</Link>
                <Link href={link({ blokker: "1" })} scroll={false} className={styles.secondaryButton}>Blokker tid</Link>
                <Link href={link({ dag: previous })} className={styles.secondaryButton} aria-label={`Forrige ${unit}`}>←</Link>
                <Link href={link({ dag: todayInOslo() })} className={styles.secondaryButton}>I dag</Link>
                <Link href={link({ dag: next })} className={styles.secondaryButton} aria-label={`Neste ${unit}`}>→</Link>
                <h2 className={calendar.dayTitle}>
                    {month ? monthTitle(day)
                        : week ? `Uke ${isoWeek(first)} · ${shortDay(first)} – ${shortDay(last)}` : formatDay(day)}
                </h2>
                <form method="get" className={calendar.datePicker}>
                    {view && <input type="hidden" name="visning" value={view}/>}
                    {employeeFilter && <input type="hidden" name="ansatt" value={employeeFilter}/>}
                    <label>
                        <span className={calendar.srOnly}>Velg dato</span>
                        <input type="date" name="dag" defaultValue={day}/>
                    </label>
                    <button type="submit" className={styles.secondaryButton}>Vis</button>
                </form>
            </div>

            {view && (
                <div className={calendar.filters}>
                    <form method="get" className={calendar.employeeFilter}>
                        <input type="hidden" name="visning" value={view}/>
                        <input type="hidden" name="dag" value={day}/>
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
                </div>
            )}

            {!view && !business.openingHours.some((h) => h.dayOfWeek === weekdayOf(day)) && (
                <p className={styles.muted}>Stengt denne dagen.</p>
            )}

            {month ? (
                <MonthGrid days={columns} month={day.slice(0, 7)} business={business}
                           dayHref={(date) => link({ dag: date, visning: undefined, ansatt: undefined })}
                           newHref={(date) => link({ dag: date, ny: "1" })}
                           hrefFor={(booking) => link({ bestilling: booking.id })}/>
            ) : (
                <TimeGrid columns={columns} range={range} showEmployee={week}
                          hrefFor={(booking) => link({ bestilling: booking.id })}
                          blockHref={(block) => link({ blokkering: block.id })}
                          newHref={(column, minute) => link({
                              ny: "1",
                              tid: clock(minute),
                              // day view: the column is an employee; week view: the column is a day
                              ...(week ? { dag: column.id } : { for: column.id }),
                          })}/>
            )}

            {/* Phone: the same visits as a simple list (the grid is too narrow there) */}
            <PhoneList columns={month ? columns.filter((c) => c.day.startsWith(day.slice(0, 7)))
                : week ? columns : [{ id: day, title: formatDay(day), day, bookings, blocks: blocksOn(blocks, day) }]}
                       hrefFor={(booking) => link({ bestilling: booking.id })}
                       blockHref={(block) => link({ blokkering: block.id })}/>

            {selected && <BookingDetails booking={selected} closeHref={here} moveHref={link({ flytt: selected.id })}/>}
            {moving && (
                <MoveBookingForm booking={moving} employees={business.employees} date={osloDate(moving.startUtc)}
                                 time={formatTime(moving.startUtc)} view={view} closeHref={link({ bestilling: moving.id })}/>
            )}            {selectedBlock && <TimeOffDetails block={selectedBlock} closeHref={here}/>}
            {newTimeOff && (
                <TimeOffForm employees={business.employees} date={day} employeeId={employeeFilter} view={view}
                             closeHref={here}/>
            )}
            {newBooking && (
                <NewBookingForm services={business.services} employees={business.employees} date={day}
                                time={newTime} employeeId={newEmployee ?? employeeFilter} view={view} closeHref={here}/>
            )}
        </main>
    );
}

// The grid: hours on the left, one column per employee (day) or per day (week)
type Href = (booking: PanelBooking) => string;

type BlockHref = (block: TimeOff) => string;

function TimeGrid({ columns, range, showEmployee, hrefFor, blockHref, newHref }: {
    columns: Column[]; range: Range; showEmployee: boolean; hrefFor: Href; blockHref: BlockHref;
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
                        {/* Blocked time: grey, under the visits, over the empty slots */}
                        {column.blocks.map((block) => {
                            const { start, end } = blockMinutes(block, column.day, range);
                            if (end <= start) return null;
                            return (
                                <Link key={block.id} href={blockHref(block)} scroll={false} className={calendar.blocked}
                                      style={{ top: (start - range.start) * PX_PER_MINUTE, height: (end - start) * PX_PER_MINUTE }}
                                      title={`${blockLabel(block, showEmployee)} – ${describeBlock(block)}`}>
                                    {blockLabel(block, showEmployee)}
                                </Link>
                            );
                        })}
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
                                    <span className={calendar.time}>
                                        {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                        <PendingBadge status={booking.status}/>
                                    </span>
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

// Month: a square per day with the number of visits, the first times and closed / blocked days;
// a click on the day number opens that day
const MONTH_PREVIEW = 3;

function MonthGrid({ days, month, business, dayHref, newHref, hrefFor }: {
    days: Column[]; month: string; business: BusinessDetails; dayHref: (date: string) => string;
    newHref: (date: string) => string; hrefFor: Href;
}) {
    const today = todayInOslo();
    const weeks: Column[][] = [];
    for (let i = 0; i < days.length; i += 7) {
        weeks.push(days.slice(i, i + 7));
    }
    return (
        <div className={calendar.scroller}>
            <div className={calendar.month}>
                <div className={calendar.corner}/>
                {["man.", "tir.", "ons.", "tor.", "fre.", "lør.", "søn."].map((name) => (
                    <div key={name} className={calendar.columnHead}>{name}</div>
                ))}
                {weeks.map((weekDays) => [
                    <div key={`uke-${weekDays[0].day}`} className={calendar.weekNumber}>{isoWeek(weekDays[0].day)}</div>,
                    ...weekDays.map((column) => {
                        const visits = [...column.bookings].sort((a, b) => a.startUtc.localeCompare(b.startUtc));
                        const closedAllDay = column.blocks.some((t) => !t.employeeId && blockHoursOn(t, column.day) === "Hele dagen");
                        const closed = closedAllDay || !business.openingHours.some((h) => h.dayOfWeek === weekdayOf(column.day));
                        const classes = [
                            calendar.monthDay,
                            column.day.startsWith(month) ? "" : calendar.otherMonth,
                            closed ? calendar.closedDay : "",
                            column.day === today ? calendar.today : "",
                        ].join(" ");
                        return (
                            <div key={column.day} className={classes}>
                                <div className={calendar.monthDayHead}>
                                    <Link href={dayHref(column.day)} className={calendar.dayNumber}
                                          aria-label={formatDay(column.day)}>{Number(column.day.slice(8))}</Link>
                                    <Link href={newHref(column.day)} scroll={false} className={calendar.monthAdd}
                                          aria-label={`Ny bestilling ${formatDay(column.day)}`}>+</Link>
                                </div>
                                {closed && <span className={calendar.closedLabel}>Stengt</span>}
                                {column.blocks.filter((t) => !closedAllDay || t.employeeId).map((block) => (
                                    <span key={block.id} className={calendar.monthBlock}>{blockLabel(block, true)}</span>
                                ))}
                                {visits.slice(0, MONTH_PREVIEW).map((booking) => (
                                    <Link key={booking.id} href={hrefFor(booking)} scroll={false}
                                          className={`${calendar.monthVisit} ${calendar[booking.status] ?? ""}`}
                                          title={`${booking.customerName} – ${booking.serviceName} hos ${booking.employeeName}`}>
                                        <PendingBadge status={booking.status} iconOnly/>
                                        {formatTime(booking.startUtc)} {booking.customerName}
                                    </Link>
                                ))}
                                {visits.length > MONTH_PREVIEW && (
                                    <Link href={dayHref(column.day)} className={calendar.monthMore}>
                                        +{visits.length - MONTH_PREVIEW} til
                                    </Link>
                                )}
                            </div>
                        );
                    }),
                ])}
            </div>
        </div>
    );
}

// Phone: the visits of each day as a list of cards; a tap opens the details window
function PhoneList({ columns, hrefFor, blockHref }: { columns: Column[]; hrefFor: Href; blockHref: BlockHref }) {
    const days = columns.filter((c) => c.bookings.length > 0 || c.blocks.length > 0);
    return (
        <div className={calendar.phoneList}>
            {days.length === 0 && <p className={styles.muted}>Ingen bestillinger.</p>}
            {days.map((column) => (
                <section key={column.id}>
                    <h3 className={calendar.phoneDay}>{column.title}</h3>
                    <ul className={styles.list}>
                        {column.blocks.map((block) => (
                            <li key={block.id}>
                                <Link href={blockHref(block)} scroll={false} className={`${calendar.phoneCard} ${calendar.phoneBlocked}`}>
                                    <span className={calendar.time}>{blockHoursOn(block, column.day)}</span>
                                    <span>{blockLabel(block, true)}</span>
                                </Link>
                            </li>
                        ))}
                        {[...column.bookings].sort((a, b) => a.startUtc.localeCompare(b.startUtc)).map((booking) => (
                            <li key={booking.id}>
                                <Link href={hrefFor(booking)} scroll={false}
                                      className={`${calendar.phoneCard} ${calendar[booking.status] ?? ""}`}>
                                    <span className={calendar.time}>
                                        {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                        <PendingBadge status={booking.status}/>
                                    </span>
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
function BookingDetails({ booking, closeHref, moveHref }: { booking: PanelBooking; closeHref: string; moveHref: string }) {
    const { canConfirm, canCancel, canMove } = allowedActions(booking);
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
                {booking.previousStartUtc && (
                    <p className={styles.muted}>
                        Flyttet fra {formatDay(osloDate(booking.previousStartUtc))} kl. {formatTime(booking.previousStartUtc)}.
                        Kunden har fått en lenke for å godta den nye tiden.
                    </p>
                )}
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
                        {canMove && (
                            <Link href={moveHref} scroll={false} className={styles.secondaryButton}>Flytt</Link>
                        )}
                        {canCancel && (
                            <details>
                                <summary className={styles.dangerButton}>Avlys</summary>
                                <p>Er du sikker?{booking.customerEmail && " Kunden får beskjed på e-post."}</p>
                                <form action={cancelBookingAction}>
                                    <input type="hidden" name="bookingId" value={booking.id}/>
                                    <input type="hidden" name="tilbake" value={closeHref}/>
                                    {booking.customerEmail && (
                                        <label className={styles.cancelMessage}>
                                            Melding til kunden (valgfritt)
                                            <textarea name="melding" maxLength={300} rows={2}
                                                      placeholder="F.eks. Beklager, Ingrid er syk."/>
                                        </label>
                                    )}
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

// The details window for blocked time: when, who, and "Fjern blokkering"
function TimeOffDetails({ block, closeHref }: { block: TimeOff; closeHref: string }) {
    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="time-off-details-title">
                <div className={calendar.dialogHead}>
                    <h2 id="time-off-details-title" className={calendar.dialogTitle}>{block.reason ?? "Blokkert tid"}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogWhen}>{describeBlock(block)}</p>
                <p className={styles.service}>{block.employeeName ?? "Hele bedriften (stengt)"}</p>
                <form action={deleteTimeOffAction} className={styles.actions}>
                    <input type="hidden" name="timeOffId" value={block.id}/>
                    <input type="hidden" name="tilbake" value={closeHref}/>
                    <SubmitButton className={styles.dangerButton} pendingText="Fjerner…">Fjern blokkering</SubmitButton>
                </form>
            </section>
        </div>
    );
}

// Bekreft while waiting; Avlys and Flytt only before the visit starts (the API refuses them afterwards)
function allowedActions(booking: PanelBooking) {
    const started = new Date(booking.startUtc).getTime() <= Date.now();
    const open = booking.status !== "Avlyst" && !started;
    return {
        canConfirm: booking.status === "Venter",
        canCancel: open,
        canMove: open,
    };
}

// Day view: the employees as columns; a booking for an employee who was removed still gets a column
// A block of the whole business (employeeId null) is shown in every column
function employeeColumns(business: BusinessDetails, bookings: PanelBooking[], blocks: TimeOff[], day: string): Column[] {
    const blocksFor = (employeeId: string) => blocksOn(blocks, day).filter((t) => !t.employeeId || t.employeeId === employeeId);
    const columns: Column[] = business.employees.map((e) => ({
        id: e.id,
        title: `${e.name} ${e.lastName}`.trim(),
        day,
        bookings: bookings.filter((b) => b.employeeId === e.id),
        blocks: blocksFor(e.id),
    }));
    for (const booking of bookings) {
        if (!columns.some((c) => c.id === booking.employeeId)) {
            columns.push({
                id: booking.employeeId,
                title: booking.employeeName,
                day,
                bookings: bookings.filter((b) => b.employeeId === booking.employeeId),
                blocks: blocksFor(booking.employeeId),
            });
        }
    }
    return columns;
}

// Week view: Monday to Sunday as columns; month view: every day of the shown weeks
function dayColumns(first: string, last: string, bookings: PanelBooking[], blocks: TimeOff[]): Column[] {
    return daysBetween(first, last).map((date) => ({
        id: date,
        title: shortDay(date),
        day: date,
        bookings: bookings.filter((b) => osloDate(b.startUtc) === date),
        blocks: blocksOn(blocks, date),
    }));
}

// Blocks that touch this day in Oslo (a holiday over several days touches each of them)
function blocksOn(blocks: TimeOff[], date: string): TimeOff[] {
    return blocks.filter((t) => osloDate(t.startUtc) <= date && (osloDate(t.endUtc) > date
        || (osloDate(t.endUtc) === date && osloMinutes(t.endUtc) > 0)));
}

// The part of a block on one day, cut to the shown hours (a whole day = from opening to closing in the grid)
function blockMinutes(block: TimeOff, date: string, range: Range): Range {
    const start = osloDate(block.startUtc) < date ? 0 : osloMinutes(block.startUtc);
    const end = osloDate(block.endUtc) > date ? 24 * 60 : osloMinutes(block.endUtc);
    return { start: Math.max(start, range.start), end: Math.min(end, range.end) };
}

// "Ferie", in the week view with the employee: "Ingrid Hansen · Ferie"
function blockLabel(block: TimeOff, withEmployee: boolean): string {
    const reason = block.reason ?? "Blokkert";
    const who = block.employeeName ?? "Hele bedriften";
    return withEmployee || !block.employeeId ? `${who} · ${reason}` : reason;
}

// The hours of a block on one day for the phone list: "10:00–12:00", "Hele dagen", "14:00–24:00"
function blockHoursOn(block: TimeOff, date: string): string {
    const from = osloDate(block.startUtc) < date ? "00:00" : formatTime(block.startUtc);
    const to = osloDate(block.endUtc) > date ? "24:00" : formatTime(block.endUtc);
    return from === "00:00" && to === "24:00" ? "Hele dagen" : `${from}–${to}`;
}

// "torsdag 22. oktober, 10:00–12:00", "torsdag 22. oktober, hele dagen", "mandag 2. – fredag 6. november"
function describeBlock(block: TimeOff): string {
    const startDay = osloDate(block.startUtc);
    const endDay = osloDate(block.endUtc);
    const wholeDays = osloMinutes(block.startUtc) === 0 && osloMinutes(block.endUtc) === 0;
    if (wholeDays) {
        const lastDay = shiftDay(endDay, -1);
        return lastDay === startDay ? `${formatDay(startDay)}, hele dagen` : `${formatDay(startDay)} – ${formatDay(lastDay)}`;
    }
    return startDay === endDay
        ? `${formatDay(startDay)}, ${formatTime(block.startUtc)}–${formatTime(block.endUtc)}`
        : `${formatDay(startDay)} ${formatTime(block.startUtc)} – ${formatDay(endDay)} ${formatTime(block.endUtc)}`;
}

// "2026-10-19" -> "man. 19.10." (short enough for a column head)
function shortDay(date: string): string {
    return new Intl.DateTimeFormat("nb-NO", { timeZone: "UTC", weekday: "short", day: "numeric", month: "numeric" })
        .format(new Date(`${date}T12:00:00Z`));
}

function weekDays(monday: string): string[] {
    return [0, 1, 2, 3, 4, 5, 6].map((n) => shiftDay(monday, n));
}

// Every day from first to last, both included
function daysBetween(first: string, last: string): string[] {
    const days: string[] = [];
    for (let date = first; date <= last; date = shiftDay(date, 1)) {
        days.push(date);
    }
    return days;
}

function mondayOf(date: string): string {
    return shiftDay(date, -((weekdayOf(date) + 6) % 7));
}

function lastDayOfMonth(date: string): string {
    return shiftDay(shiftMonth(date, 1), -1);
}

// The 1st of the month n months later: ("2026-10-22", 1) -> "2026-11-01"
function shiftMonth(date: string, n: number): string {
    const d = new Date(Date.UTC(Number(date.slice(0, 4)), Number(date.slice(5, 7)) - 1 + n, 1));
    return d.toISOString().slice(0, 10);
}

// "2026-10-22" -> "Oktober 2026"
function monthTitle(date: string): string {
    const title = new Intl.DateTimeFormat("nb-NO", { timeZone: "UTC", month: "long", year: "numeric" })
        .format(new Date(`${date.slice(0, 7)}-15T12:00:00Z`));
    return title.charAt(0).toUpperCase() + title.slice(1);
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


