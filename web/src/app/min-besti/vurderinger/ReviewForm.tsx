"use client";
import { Fragment, useActionState } from "react";
import { createReviewAction } from "../actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

// Stars and comment for one visit; the API's own message (e.g. already reviewed) shows here when it fails
export default function ReviewForm({ bookingId }: { bookingId: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(createReviewAction, {});

    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={panelStyles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="bookingId" value={bookingId}/>
            <p className={styles.label}>Hvor fornøyd var du?</p>
            {/* Stars 1..5 for keyboard and screen readers; CSS lights up the chosen one and those before it */}
            <div className={styles.stars} role="radiogroup" aria-label="Antall stjerner">
                {[1, 2, 3, 4, 5].map((n) => (
                    <Fragment key={n}>
                        <input type="radio" id={`s${n}-${bookingId}`} name="stars" value={n} required
                               defaultChecked={state.values?.stars === String(n)}/>
                        <label htmlFor={`s${n}-${bookingId}`} title={`${n} av 5`}>
                            ★<span className={styles.srOnly}>{n} av 5 stjerner</span>
                        </label>
                    </Fragment>
                ))}
            </div>
            <label className={styles.label}>
                Kommentar (valgfritt)
                <textarea
                    name="comment"
                    maxLength={500}
                    rows={3}
                    className={styles.textarea}
                    placeholder="Hva var bra? Hva kan bli bedre?"
                    defaultValue={state.values?.comment}
                />
            </label>
            <SubmitButton className={panelStyles.primaryButton} pendingText="Sender…">
                Send vurdering
            </SubmitButton>
        </form>
    );
}
