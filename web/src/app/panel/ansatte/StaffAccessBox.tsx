"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import type { StaffAccess } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { inviteStaffAction, removeStaffAction } from "./actions";
import styles from "../tjenester/page.module.css";

// "Egen innlogging": the employee sees their own visits under /ansatt with their own BESTI account
// (no prices, statistics or settings). Invite by e-mail, see who has accepted, take the access away
export default function StaffAccessBox({ employeeId, name, access }: {
    employeeId: string; name: string; access: StaffAccess | undefined
}) {
    const [state, formAction] = useActionState<FormState, FormData>(inviteStaffAction, {});

    if (access?.connected) {
        return (
            <div className={styles.form}>
                <p>✓ {name} logger inn med <strong>{access.email}</strong> og ser sine egne timer.</p>
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
                <p className={styles.muted}>
                    Invitasjon sendt til {access.email}. Venter på at {name} åpner lenken i e-posten.
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
                    {access?.email ? "Send på nytt" : "Send invitasjon"}
                </SubmitButton>
            </form>
            {access?.email && (
                <form action={removeStaffAction}>
                    <input type="hidden" name="employeeId" value={employeeId}/>
                    <SubmitButton className={styles.secondaryButton} pendingText="Trekker…">Trekk tilbake</SubmitButton>
                </form>
            )}
        </div>
    );
}
