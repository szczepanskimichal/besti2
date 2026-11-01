"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./ansatte.module.css";

type EmployeeFormProps = {
    className?: string;
    action: (previous: FormState, formData: FormData) => Promise<FormState>;
    employeeId?: string;
    name?: string;
    lastName?: string;
    submitText: string;
};

// Add or edit an employee: on an error the message shows here and the names stay
export default function EmployeeForm({ className, action, employeeId, name = "", lastName = "", submitText }: EmployeeFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(action, {});

    return (
        <form action={formAction} className={className ? `${styles.form} ${className}` : styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            {employeeId && <input type="hidden" name="employeeId" value={employeeId} />}
            <label className={styles.field}>
                Fornavn
                <input name="name" required maxLength={200} defaultValue={state.values?.name ?? name} />
            </label>
            <label className={styles.field}>
                Etternavn (valgfritt)
                <input name="lastName" maxLength={200} defaultValue={state.values?.lastName ?? lastName} />
            </label>
            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">
                {submitText}
            </SubmitButton>
        </form>
    );
}