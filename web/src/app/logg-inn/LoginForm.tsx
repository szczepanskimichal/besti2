"use client";
import { useActionState } from "react";
import { loginAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

// The login form: on an error it stays on the page with the e-mail and the chosen time (neste) kept
export default function LoginForm({ neste }: { neste: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(loginAction, {});
    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="neste" value={neste} />
            <label className={styles.field}>
                E-post
                <input name="email" type="email" required autoComplete="email"
                       defaultValue={state.values?.email ?? ""} />
            </label>
            <label className={styles.field}>
                Passord
                <input name="password" type="password" required autoComplete="current-password" />
            </label>
            <SubmitButton className={styles.primaryButton} pendingText="Logger inn…">
                Logg inn
            </SubmitButton>
        </form>
    );
}