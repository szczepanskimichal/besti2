import Link from "next/link";
import { formatDay, formatTime, osloDay } from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import type { PanelBooking } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { cancelBookingAction, confirmBookingAction } from "../actions";
import { NoShowButtons, NoShowCount } from "../NoShow";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";

// The details window for one visit: customer, contact, note and Bekreft / Avlys
export default function BookingDetails({ booking, closeHref, moveHref }: { booking: PanelBooking; closeHref: string; moveHref: string }) {
    const { started, canConfirm, canCancel, canMove } = allowedActions(booking);
    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="booking-title">
                <div className={calendar.dialogHead}>
                    <h2 id="booking-title" className={calendar.dialogTitle}>{booking.customerName}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogWhen}>
                    {formatDay(osloDay(booking.startUtc))}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                </p>
                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
                    {statusLabels[booking.status] ?? booking.status}
                </span>
                <p className={styles.service}>{booking.serviceName} hos {booking.employeeName}</p>
                {booking.previousStartUtc && (
                    <p className={styles.muted}>
                        Flyttet fra {formatDay(osloDay(booking.previousStartUtc))} kl. {formatTime(booking.previousStartUtc)}.
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
                <NoShowCount count={booking.noShowCount}/>
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
                {started && <NoShowButtons booking={booking} back={closeHref}/>}
            </section>
        </div>
    );
}

// Bekreft while waiting; Avlys and Flytt only before the visit starts (the API refuses them afterwards)
export function allowedActions(booking: PanelBooking) {
    const started = new Date(booking.startUtc).getTime() <= Date.now();
    const open = booking.status !== "Avlyst" && !started;
    return {
        started,
        // After the start time "Møtte" / "Møtte ikke" take over from "Bekreft"
        canConfirm: booking.status === "Venter" && !started,
        canCancel: open,
        canMove: open,
    };
}
