"use client";
import { useActionState } from "react";
import { weekdays } from "@/lib/weekdays";
import { updateOpeningHoursAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../tjenester/page.module.css";
import dayStyles from "./page.module.css";

type OpeningHoursFormProps = {
    // The saved hours, e.g. { "open-1": "on", "opensAt-1": "09:00", "closesAt-1": "17:00", "breakStart-1": "12:00" }
    initial: Record<string, string>;
};

// All 7 days in one form: on an error the message shows here and every day stays as typed
export default function OpeningHoursForm({ initial }: OpeningHoursFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(updateOpeningHoursAction, {});
    const values = state.values ?? initial;

    return (
        // key: after an error the form is drawn again from the typed values (checkboxes included)
        <form key={JSON.stringify(values)} action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            {weekdays.map((w) => (
                <fieldset key={w.day} className={dayStyles.dayRow}>
                    <legend>{w.label}</legend>
                    <label className={dayStyles.checkbox}>
                        <input type="checkbox" name={`open-${w.day}`} defaultChecked={values[`open-${w.day}`] === "on"} />
                        Åpen
                    </label>
                    <label className={styles.field}>
                        Fra
                        <input type="time" name={`opensAt-${w.day}`} defaultValue={values[`opensAt-${w.day}`] ?? "09:00"} />
                    </label>
                    <label className={styles.field}>
                        Til
                        <input type="time" name={`closesAt-${w.day}`} defaultValue={values[`closesAt-${w.day}`] ?? "17:00"} />
                    </label>
                    <label className={styles.field}>
                        Pause fra
                        <input type="time" name={`breakStart-${w.day}`} defaultValue={values[`breakStart-${w.day}`] ?? ""} />
                    </label>
                    <label className={styles.field}>
                        Pause til
                        <input type="time" name={`breakEnd-${w.day}`} defaultValue={values[`breakEnd-${w.day}`] ?? ""} />
                    </label>
                </fieldset>
            ))}
            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">
                Lagre åpningstider
            </SubmitButton>
        </form>
    );
}