import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBookings, getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { clockMinutes, formatDay, formatTime, isIsoDate, osloMinutes, shiftDay, todayInOslo, weekdayOf } from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import type { Employee, PanelBooking } from "@/lib/types";
import PanelNav from "./PanelNav";
import styles from "./page.module.css";
import calendar from "./calendar.module.css";

// One hour in the grid is 80 px high (a 30 min visit = 40 px: time and customer fit)
const PX_PER_MINUTE = 80 / 60;

type CalendarPageProps = {
    searchParams: Promise<{ dag?: string; feil?: string }>;
};

// The business calendar: one day, a column per employee, visits as blocks from start to end
export default async function CalendarPage({ searchParams }: CalendarPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const { dag, feil } = await searchParams;
    const day = isIsoDate(dag) ? dag : todayInOslo();
    const [business, bookings] = await Promise.all([getPanelBusiness(token), getPanelBookings(token, day, day)]);
    if (!business || !bookings) {
        redirect("/logg-inn");
    }

    const columns = calendarColumns(business.employees, bookings);
    const hours = business.openingHours.find((h) => h.dayOfWeek === weekdayOf(day));
    const range = visibleRange(hours ? { opens: clockMinutes(hours.opensAt), closes: clockMinutes(hours.closesAt) } : null, bookings);
    const height = (range.end - range.start) * PX_PER_MINUTE;

    return (
        <main className={`${styles.main} ${calendar.wide}`}>
            <PanelNav title="Kalender"/>

            {feil && (
                <p className={styles.error} role="alert">
                    {feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}

            <div className={calendar.toolbar}>
                <Link href={`/panel?dag=${shiftDay(day, -1)}`} className={styles.secondaryButton} aria-label="Forrige dag">←</Link>
                <Link href="/panel" className={styles.secondaryButton}>I dag</Link>
                <Link href={`/panel?dag=${shiftDay(day, 1)}`} className={styles.secondaryButton} aria-label="Neste dag">→</Link>
                <h2 className={calendar.dayTitle}>{formatDay(day)}</h2>
                <form method="get" className={calendar.datePicker}>
                    <label>
                        <span className={calendar.srOnly}>Velg dato</span>
                        <input type="date" name="dag" defaultValue={day}/>
                    </label>
                    <button type="submit" className={styles.secondaryButton}>Vis</button>
                </form>
            </div>

            {!hours && <p className={styles.muted}>Stengt denne dagen.</p>}

            <div className={calendar.scroller}>
                <div className={calendar.grid} style={{ gridTemplateColumns: `56px repeat(${columns.length}, minmax(160px, 1fr))` }}>
                    {/* Header row: one name per column */}
                    <div className={calendar.corner}/>
                    {columns.map((column) => (
                        <div key={column.id} className={calendar.columnHead}>{column.name}</div>
                    ))}

                    {/* Hour labels on the left */}
                    <div className={calendar.hours} style={{ height }}>
                        {hourMarks(range).map((minute) => (
                            <span key={minute} style={{ top: (minute - range.start) * PX_PER_MINUTE }}>
                                {String(minute / 60).padStart(2, "0")}:00
                            </span>
                        ))}
                    </div>

                    {/* One column per employee with the visits as blocks */}
                    {columns.map((column) => (
                        <div key={column.id} className={calendar.column} style={{ height }}>
                            {hourMarks(range).map((minute) => (
                                <div key={minute} className={calendar.hourLine} style={{ top: (minute - range.start) * PX_PER_MINUTE }}/>
                            ))}
                            {bookings.filter((b) => b.employeeId === column.id).map((booking) => {
                                const start = Math.max(osloMinutes(booking.startUtc), range.start);
                                const end = Math.min(osloMinutes(booking.endUtc) || 24 * 60, range.end);
                                return (
                                    <div
                                        key={booking.id}
                                        className={`${calendar.booking} ${calendar[booking.status] ?? ""}`}
                                        style={{ top: (start - range.start) * PX_PER_MINUTE, height: Math.max(end - start, 15) * PX_PER_MINUTE }}
                                        title={`${booking.customerName} – ${booking.serviceName} (${statusLabels[booking.status] ?? booking.status})`}
                                    >
                                        <span className={calendar.time}>{formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}</span>
                                        <span className={calendar.customer}>{booking.customerName}</span>
                                        <span className={calendar.service}>{booking.serviceName}</span>
                                    </div>
                                );
                            })}
                        </div>
                    ))}
                </div>
            </div>
        </main>
    );
}

// The employees as columns; a booking for an employee who was removed still gets a column
function calendarColumns(employees: Employee[], bookings: PanelBooking[]) {
    const columns = employees.map((e) => ({ id: e.id, name: `${e.name} ${e.lastName}`.trim() }));
    for (const booking of bookings) {
        if (!columns.some((c) => c.id === booking.employeeId)) {
            columns.push({ id: booking.employeeId, name: booking.employeeName });
        }
    }
    return columns;
}

// From the opening hour to the closing hour, widened to whole hours and to visits outside the hours
function visibleRange(hours: { opens: number; closes: number } | null, bookings: PanelBooking[]) {
    let start = hours?.opens ?? 8 * 60;
    let end = hours?.closes ?? 17 * 60;
    for (const booking of bookings) {
        start = Math.min(start, osloMinutes(booking.startUtc));
        end = Math.max(end, osloMinutes(booking.endUtc) || 24 * 60);
    }
    return { start: Math.floor(start / 60) * 60, end: Math.min(Math.ceil(end / 60) * 60, 24 * 60) };
}

// 480, 540, 600 ... for every whole hour in the range
function hourMarks(range: { start: number; end: number }) {
    const marks: number[] = [];
    for (let minute = range.start; minute < range.end; minute += 60) {
        marks.push(minute);
    }
    return marks;
}
