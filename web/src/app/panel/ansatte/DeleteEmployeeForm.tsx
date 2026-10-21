"use client";
import { useActionState } from "react";
import { deleteEmployeeAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../tjenester/page.module.css";

// "Ja, slett": if the API refuses, the reason shows right under the button
export default function DeleteEmployeeForm({ employeeId }: { employeeId: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(deleteEmployeeAction, {});

    return (
        <form action={formAction}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="employeeId" value={employeeId} />
            <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                Ja, slett
            </SubmitButton>
        </form>
    );
}