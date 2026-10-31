"use client";
import { useActionState, useState } from "react";
import { setRebookReminderAction, type RebookFormState } from "../actions";
import styles from "./page.module.css";

// The two most common choices; "Velg selv" picks any number (the API takes 1 to 26 weeks)
const WEEKS = [4, 6];
const MIN_WEEKS = 1;
const MAX_WEEKS = 26;

// "Tid for ny time?": one tap, and BESTI reminds the customer N weeks after each visit (bell + phone, no e-mail)
export default function RebookReminderForm({ serviceId, employeeId }: { serviceId: string; employeeId: string }) {
    const [state, action, pending] = useActionState<RebookFormState, FormData>(setRebookReminderAction, {});
    // null = the − N uker + row is closed
    const [chosen, setChosen] = useState<number | null>(null);

    if (state.weeks !== undefined) {
        return (
            <p className={styles.rebookSaved} role="status">
                {state.weeks === 0
                    ? "Greit, ingen påminnelse."
                    : `Avtale! Vi minner deg på det ${state.weeks} ${state.weeks === 1 ? "uke" : "uker"} etter hver time. Du kan endre det i Min BESTI.`}
            </p>
        );
    }
    return (
        <form action={action} className={styles.rebook}>
            <p className={styles.rebookTitle}>Vil du ha en påminnelse om å bestille igjen?</p>
            {state.error && <p role="alert">{state.error}</p>}
            <input type="hidden" name="serviceId" value={serviceId}/>
            <input type="hidden" name="employeeId" value={employeeId}/>
            <div className={styles.rebookChoices}>
                {WEEKS.map((weeks) => (
                    <button key={weeks} type="submit" name="weeks" value={weeks} disabled={pending}
                            className={styles.rebookChoice}>
                        Om {weeks} uker
                    </button>
                ))}
                <button type="button" aria-expanded={chosen !== null} disabled={pending}
                        className={`${styles.rebookChoice} ${chosen !== null ? styles.rebookOpen : ""}`}
                        onClick={() => setChosen(chosen === null ? 5 : null)}>
                    Velg selv
                </button>
                <button type="submit" name="weeks" value={0} disabled={pending} className={styles.rebookChoice}>
                    Nei takk
                </button>
                {chosen !== null && (
                    <div className={styles.stepper}>
                        <button type="button" className={styles.stepButton} aria-label="Færre uker"
                                disabled={chosen <= MIN_WEEKS} onClick={() => setChosen(chosen - 1)}>−</button>
                        <b aria-live="polite">{chosen} {chosen === 1 ? "uke" : "uker"}</b>
                        <button type="button" className={styles.stepButton} aria-label="Flere uker"
                                disabled={chosen >= MAX_WEEKS} onClick={() => setChosen(chosen + 1)}>+</button>
                        <button type="submit" name="weeks" value={chosen} disabled={pending} className={styles.stepSave}>
                            Lagre
                        </button>
                    </div>
                )}
            </div>
        </form>
    );
}
