"use client";
import { useActionState } from "react";
import { formatDay } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { joinWaitlistAction, type WaitlistFormState } from "./actions";
import styles from "./page.module.css";

// "Si fra hvis det blir ledig": the customer picks one of the days in the strip and gets a message in the bell
// and on the phone as soon as someone cancels. Nobody else sees the list
export default function WaitlistForm({ serviceId, employeeId, days, selectedDay }: {
    serviceId: string;
    employeeId: string | null;
    days: string[];
    selectedDay: string | undefined;
}) {
    const [state, action] = useActionState<WaitlistFormState, FormData>(joinWaitlistAction, {});
    const date = state.values?.date ?? selectedDay ?? days[0];

    return (
        <section className={styles.waitlist}>
            <h2 className={styles.sectionTitle}>Passer ingen av tidene? <span aria-hidden="true">🔔</span></h2>
            {state.saved ? (
                <p className={styles.success} role="status">
                    Du står på ventelisten for {formatDay(date)}. Vi sier fra med en gang det blir ledig, i
                    varslene og på telefonen. Du finner den i Min Besti.
                </p>
            ) : (
                <form action={action} className={styles.waitlistForm}>
                    <p className={styles.muted}>Få beskjed hvis noen avbestiller. Først til mølla!</p>
                    {state.error && <p className={styles.error} role="alert">{state.error}</p>}
                    <input type="hidden" name="serviceId" value={serviceId}/>
                    <input type="hidden" name="employeeId" value={employeeId ?? ""}/>
                    <label className={styles.field}>
                        Hvilken dag?
                        <select name="date" defaultValue={date}>
                            {days.map((day) => <option key={day} value={day}>{formatDay(day)}</option>)}
                        </select>
                    </label>
                    <SubmitButton className={styles.secondaryButton} pendingText="Lagrer…">
                        Si fra hvis det blir ledig
                    </SubmitButton>
                </form>
            )}
        </section>
    );
}
