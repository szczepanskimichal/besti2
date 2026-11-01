import { redirect } from "next/navigation";
import { getPanelBookings } from "@/lib/api";
import type { PanelBooking } from "@/lib/types";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime, osloDay, shiftDay, todayInOslo } from "@/lib/format";
import { customerLabel } from "@/lib/bookingStatus";
import PendingBadge, { OkStamp } from "../PendingBadge";
import { cancelBookingAction, confirmBookingAction } from "../actions";
import PanelNav from "../PanelNav";
import { NoShowButtons, NoShowCount } from "../NoShow";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../page.module.css";
import liste from "./liste.module.css";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import EmptyState from "@/components/EmptyState/EmptyState";

type PanelPageProps = {
    searchParams: Promise<{ feil?: string }>;
};

export default async function PanelListPage({ searchParams }: PanelPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const bookings = await getPanelBookings(token);
    if (!bookings) {
        redirect("/logg-inn");
    }

    const { feil } = await searchParams;
    // Cancelled visits are not shown at all
    const { upcoming, past } = splitBookings(bookings.filter((b) => b.status !== "Avlyst"));

    return (
        <main className={styles.main}>
            <PanelNav title="Bestillinger" active="Liste"/>

            {feil && (
                <p className={styles.error} role="alert">
                    {feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}

            {upcoming.length === 0 ? (
                <EmptyState icon="🗓️" title="Ingen kommende bestillinger"
                            action={{ href: "/panel/del", label: "Del lenken din" }}>
                    Når kundene bestiller, kommer timene her. Del lenken eller heng opp plakaten, så finner de deg.
                </EmptyState>
            ) : (
                <DayGroups bookings={upcoming} showActions/>
            )}

            {past.length > 0 && (
                <>
                    <h2 className={liste.sectionTitle}>Siste 14 dager</h2>
                    <DayGroups bookings={past} showActions={false} past/>
                </>
            )}
        </main>
    );
}

// Visits that are over come last, newest first, without buttons (they can no longer be cancelled)
function splitBookings(bookings: PanelBooking[]) {
    const now = Date.now();
    return {
        upcoming: bookings.filter((b) => new Date(b.endUtc).getTime() >= now)
            .sort((a, b) => a.startUtc.localeCompare(b.startUtc)),
        past: bookings.filter((b) => new Date(b.endUtc).getTime() < now)
            .sort((a, b) => b.startUtc.localeCompare(a.startUtc)),
    };
}

// After the start time "Møtte" / "Møtte ikke" take over from "Bekreft" / "Avlys"
function hasStarted(booking: PanelBooking) {
    return new Date(booking.startUtc).getTime() <= Date.now();
}

// Visits grouped by day under a heading: "I dag", "I morgen", "I går" or the date
function DayGroups({ bookings, showActions, past = false }: {
    bookings: PanelBooking[]; showActions: boolean; past?: boolean;
}) {
    const groups = new Map<string, PanelBooking[]>();
    for (const booking of bookings) {
        const day = osloDay(booking.startUtc);
        groups.set(day, [...(groups.get(day) ?? []), booking]);
    }
    return (
        <div className={past ? `${liste.days} ${liste.past}` : liste.days}>
            {[...groups.entries()].map(([day, dayBookings]) => (
                <section key={day} className={liste.day}>
                    <h3 className={liste.dayTitle}>
                        <span>{dayName(day)}</span>
                        <small>{dateWithoutWeekday(dayBookings[0].startUtc)}</small>
                        <span className={liste.dayCount}>
                            {dayBookings.length} {dayBookings.length === 1 ? "time" : "timer"}
                        </span>
                    </h3>
                    <ul className={liste.cards}>
                        {dayBookings.map((booking) => (
                            <BookingCard key={booking.id} booking={booking} showActions={showActions}/>
                        ))}
                    </ul>
                </section>
            ))}
        </div>
    );
}

// "lørdag 31. oktober" -> "31. oktober" (the weekday is already in the heading)
function dateWithoutWeekday(utc: string) {
    return formatDate(utc).split(" ").slice(1).join(" ");
}

function dayName(day: string) {
    const today = todayInOslo();
    if (day === today) return "I dag";
    if (day === shiftDay(today, 1)) return "I morgen";
    if (day === shiftDay(today, -1)) return "I går";
    const weekday = formatDate(`${day}T12:00:00Z`).split(" ")[0];
    return weekday.charAt(0).toUpperCase() + weekday.slice(1);
}

// One booking like in the calendar: big time on the left, a neon frame in the status colour, a round stamp,
// then the customer's contact, note and Bekreft / Avlys (or Møtte / Møtte ikke once the visit has started)
function BookingCard({ booking, showActions }: { booking: PanelBooking; showActions: boolean }) {
    const started = hasStarted(booking);
    return (
        <li className={`${liste.card} ${liste[booking.status] ?? ""}`}>
            <div className={liste.top}>
                <span className={liste.time}>
                    <strong>{formatTime(booking.startUtc)}</strong>
                    <small>{formatTime(booking.endUtc)}</small>
                </span>
                <span className={liste.body}>
                    <span className={liste.who}>{customerLabel(booking)}</span>
                    <span className={liste.service}><CrystalIcon name="stjerne"/>{booking.serviceName}</span>
                </span>
                <span className={liste.stamp}>
                    {booking.status === "Bekreftet" ? <OkStamp/> : <PendingBadge status={booking.status}/>}
                    {booking.status === "MotteIkke" && <span className={liste.missed}>Møtte ikke</span>}
                </span>
            </div>

            <p className={liste.contact}>
                <a href={`tel:${booking.customerPhone}`}>{booking.customerPhone}</a>
                {booking.customerEmail && <a href={`mailto:${booking.customerEmail}`}>{booking.customerEmail}</a>}
            </p>
            <NoShowCount count={booking.noShowCount}/>
            {booking.customerNote && (
                <div className={liste.note}>
                    <p>{booking.customerNote}</p>
                    {booking.customerEmail && (
                        <a href={`mailto:${booking.customerEmail}?subject=${encodeURIComponent("Svar på bestillingen din")}`}>
                            Svar på e-post
                        </a>
                    )}
                </div>
            )}
            <p className={liste.employee}>
                <span className={liste.avatar} aria-hidden="true">{booking.employeeName.charAt(0)}</span>
                <span><small>Hos</small> {booking.employeeName}</span>
            </p>
            {showActions && !started && booking.status !== "Avlyst" && (
                <div className={styles.actions}>
                    {booking.status === "Venter" && (
                        <form action={confirmBookingAction}>
                            <input type="hidden" name="bookingId" value={booking.id} />
                            <input type="hidden" name="tilbake" value="/panel/liste" />
                            <SubmitButton className={styles.primaryButton} pendingText="Bekrefter…">
                                Bekreft
                            </SubmitButton>
                        </form>
                    )}
                    <details className={liste.cancel}>
                        <summary className={styles.dangerButton}>Avlys</summary>
                        <p>Er du sikker?{booking.customerEmail && " Kunden får beskjed på e-post."}</p>
                        <form action={cancelBookingAction}>
                            <input type="hidden" name="bookingId" value={booking.id} />
                            <input type="hidden" name="tilbake" value="/panel/liste" />
                            {booking.customerEmail && (
                                <label className={styles.cancelMessage}>
                                    Melding til kunden (valgfritt)
                                    <textarea name="melding" maxLength={300} rows={2}
                                              placeholder="F.eks. Beklager, Ingrid er syk."/>
                                </label>
                            )}
                            <SubmitButton className={styles.dangerButton} pendingText="Avlyser…">
                                Ja, avlys
                            </SubmitButton>
                        </form>
                    </details>
                </div>
            )}
            {started && <NoShowButtons booking={booking} back="/panel/liste"/>}
        </li>
    );
}
