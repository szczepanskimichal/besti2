"use client";
import { useActionState } from "react";
import { cancelMyBookingAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import CloseDetailsButton from "@/components/CloseDetailsButton/CloseDetailsButton";
import styles from "../panel/page.module.css";

// "Ja, avbestill" with an optional reason; the API's own message shows here when it fails
export default function CancelBookingForm({ bookingId }: { bookingId: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(cancelMyBookingAction, {});

    return (
        <form action={formAction}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="bookingId" value={bookingId}/>
            <label className={styles.cancelMessage}>
                Grunn til bedriften (valgfritt)
                <textarea name="melding" maxLength={300} rows={2} defaultValue={state.values?.melding}
                          placeholder="F.eks. Jeg er dessverre syk."/>
            </label>
            <div className={styles.actions}>
                <SubmitButton className={styles.dangerButton} pendingText="Avbestiller…">
                    Ja, avbestill
                </SubmitButton>
                <CloseDetailsButton className={styles.secondaryButton}>
                    Nei, behold timen
                </CloseDetailsButton>
            </div>
        </form>
    );
}
