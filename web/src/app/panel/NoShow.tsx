import SubmitButton from "@/components/SubmitButton/SubmitButton";
import type { PanelBooking } from "@/lib/types";
import { confirmBookingAction, noShowAction } from "./actions";
import styles from "./page.module.css";

// How many times this customer did not come to this business (only this business sees it)
export function NoShowCount({ count }: { count: number }) {
    if (count === 0) {
        return null;
    }
    return <p className={styles.noShow}>Møtte ikke: {count} {count === 1 ? "gang" : "ganger"}</p>;
}

// After the start time: did the customer come? "Møtte" = Bekreftet, "Møtte ikke" = MotteIkke
// (a customer with an account gets an e-mail about "Møtte ikke"; "Venter på kunden" can be marked too)
export function NoShowButtons({ booking, back }: { booking: PanelBooking; back: string }) {
    const canCome = ["Venter", "VenterKunde", "MotteIkke"].includes(booking.status);
    const canMiss = ["Venter", "VenterKunde", "Bekreftet"].includes(booking.status);
    if (!canCome && !canMiss) {
        return null;
    }

    return (
        <div className={styles.actions}>
            {canCome && (
                <form action={confirmBookingAction}>
                    <input type="hidden" name="bookingId" value={booking.id}/>
                    <input type="hidden" name="tilbake" value={back}/>
                    <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Møtte</SubmitButton>
                </form>
            )}
            {canMiss && (
                <form action={noShowAction}>
                    <input type="hidden" name="bookingId" value={booking.id}/>
                    <input type="hidden" name="tilbake" value={back}/>
                    <SubmitButton className={styles.dangerButton} pendingText="Lagrer…">Møtte ikke</SubmitButton>
                </form>
            )}
        </div>
    );
}
