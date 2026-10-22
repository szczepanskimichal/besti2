import { redirect } from "next/navigation";
import { getPanelBookings } from "@/lib/api";
import type { PanelBooking } from "@/lib/types";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime } from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import { cancelBookingAction, confirmBookingAction } from "../actions";
import PanelNav from "../PanelNav";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../page.module.css";

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
    const { upcoming, past } = splitBookings(bookings);

    return (
        <main className={styles.main}>
            <PanelNav title="Bestillinger"/>

            {feil && (
                <p className={styles.error} role="alert">
                    {feil === "for-sent"
                        ? "Bestillingen kan ikke avlyses lenger – timen har allerede startet."
                        : "Kunne ikke endre bestillingen. Prøv igjen."}
                </p>
            )}

            {upcoming.length === 0 ? (
                <p className={styles.muted}>Ingen kommende bestillinger.</p>
            ) : (
                <ul className={styles.list}>
                    {upcoming.map((booking) => (
                        <BookingCard key={booking.id} booking={booking} showActions/>
                    ))}
                </ul>
            )}

            {past.length > 0 && (
                <>
                    <h2 className={styles.sectionTitle}>Siste 14 dager</h2>
                    <ul className={styles.list}>
                        {past.map((booking) => (
                            <BookingCard key={booking.id} booking={booking} showActions={false}/>
                        ))}
                    </ul>
                </>
            )}
        </main>
    );
}

// Visits that are over come last, newest first, without buttons (they can no longer be cancelled)
function splitBookings(bookings: PanelBooking[]) {
    const now = Date.now();
    return {
        upcoming: bookings.filter((b) => new Date(b.endUtc).getTime() >= now),
        past: bookings.filter((b) => new Date(b.endUtc).getTime() < now).reverse(),
    };
}

// One booking: time, status, service, customer, note, and Bekreft / Avlys for upcoming visits
function BookingCard({ booking, showActions }: { booking: PanelBooking; showActions: boolean }) {
    return (
        <li className={styles.card}>
            <div className={styles.cardHeader}>
                <p className={styles.when}>
                    {formatDate(booking.startUtc)}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                </p>
                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
                    {statusLabels[booking.status] ?? booking.status}
                </span>
            </div>

            <p className={styles.service}>
                {booking.serviceName} hos {booking.employeeName}
            </p>

            <p className={styles.customer}>
                {booking.customerName}
                <br />
                <a href={`tel:${booking.customerPhone}`}>{booking.customerPhone}</a>
                {booking.customerEmail && (
                    <>
                        {" · "}
                        <a href={`mailto:${booking.customerEmail}`}>{booking.customerEmail}</a>
                    </>
                )}            </p>
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
            {showActions && booking.status !== "Avlyst" && (
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
                    <details>
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
        </li>
    );
}
