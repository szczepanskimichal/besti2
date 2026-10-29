"use client";
import { useActionState } from "react";
import { setRebookReminderAction, type RebookFormState } from "../actions";
import styles from "./page.module.css";

// Same choices as the API (RebookReminderService.WeekOptions)
const WEEKS = [3, 4, 6, 8, 12];

// "Tid for ny time?": one tap, and BESTI reminds the customer N weeks after each visit (bell + phone, no e-mail)
export default function RebookReminderForm({ serviceId, employeeId }: { serviceId: string; employeeId: string }) {
    const [state, action, pending] = useActionState<RebookFormState, FormData>(setRebookReminderAction, {});

    if (state.weeks !== undefined) {
        return (
            <p className={styles.rebookSaved} role="status">
                {state.weeks === 0
                    ? "Greit, ingen påminnelse."
                    : `Avtale! Vi minner deg på det ${state.weeks} uker etter hver time. Du kan endre det i Min Besti.`}
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
                <button type="submit" name="weeks" value={0} disabled={pending} className={styles.rebookNo}>
                    Nei takk
                </button>
            </div>
        </form>
    );
}
