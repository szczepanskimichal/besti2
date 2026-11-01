"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import type { StaffAccess } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { inviteStaffAction, removeStaffAction } from "./actions";
import styles from "./ansatte.module.css";

// "Egen innlogging": the employee sees their own visits under /ansatt with their own BESTI account
// (no prices, statistics or settings). Invite by e-mail, see who has accepted, take the access away
export default function StaffAccessBox({ employeeId, name, access }: {
    employeeId: string; name: string; access: StaffAccess | undefined
}) {
    const [state, formAction] = useActionState<FormState, FormData>(inviteStaffAction, {});

    if (access?.connected) {
        return (
            <div className={styles.form}>
                <p>{name} har tilgang til jobbkalenderen med <strong>{access.email}</strong>.</p>
                <form action={removeStaffAction}>
                    <input type="hidden" name="employeeId" value={employeeId}/>
                    <SubmitButton className={styles.dangerButton} pendingText="Fjerner…">Fjern tilgang</SubmitButton>
                </form>
            </div>
        );
    }

    return (
        <div className={styles.form}>
            {access?.email && (
                <p>
                    Lenke til jobbkalenderen er sendt til <strong>{access.email}</strong>. Venter på at {name} åpner den.
                </p>
            )}
            <form action={formAction} className={styles.form}>
                {state.error && <p className={styles.error} role="alert">{state.error}</p>}
                <input type="hidden" name="employeeId" value={employeeId}/>
                <label className={styles.field}>
                    {name}s egen e-post
                    <input name="email" type="email" required maxLength={250}
                           defaultValue={state.values?.email ?? access?.email ?? ""} autoComplete="off"/>
                </label>
                <SubmitButton className={styles.primaryButton} pendingText="Sender…">
                    {access?.email ? "Send på nytt" : "Gi tilgang"}
                </SubmitButton>
            </form>
            {access?.email && (
                <form action={removeStaffAction} className={styles.buttons}>
                    <input type="hidden" name="employeeId" value={employeeId}/>
                    <SubmitButton className={styles.dangerButton} pendingText="Trekker…">Trekk tilbake</SubmitButton>
                </form>
            )}
        </div>
    );
}
