"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { customerNoteAction } from "../actions";
import styles from "../page.module.css";

// The business's own note about the customer; shown in the calendar next to every visit
export default function CustomerNoteForm({ customerKey, note }: { customerKey: string; note: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(customerNoteAction, {});
    return (
        <form action={formAction} className={styles.noteForm}>
            {state.error && <p className={styles.error} role="alert">{state.error}</p>}
            <input type="hidden" name="key" value={customerKey}/>
            <label className={styles.srOnly} htmlFor="kunde-notat">Notat</label>
            <textarea id="kunde-notat" name="note" rows={4} maxLength={1000}
                      defaultValue={state.values?.note ?? note} className={styles.textarea}
                      placeholder="F.eks. «Liker det kort i nakken. Allergisk mot ammoniakk. Vil ha te, ikke kaffe.»"/>
            <SubmitButton className={styles.saveButton} pendingText="Lagrer…">Lagre notat</SubmitButton>
        </form>
    );
}
