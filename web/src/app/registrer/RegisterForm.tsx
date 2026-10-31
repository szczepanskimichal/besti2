"use client";
import { useActionState } from "react";
import NextLink from "next/link";
import { registerCustomerAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../logg-inn/page.module.css";

// The sign-up form: on an error the name and e-mail stay, and so does the chosen time (neste)
export default function RegisterForm({ neste }: { neste: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(registerCustomerAction, {});
    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="neste" value={neste} />
            <label className={styles.field}>
                Fullt navn
                <input name="fullName" required maxLength={100} autoComplete="name"
                       defaultValue={state.values?.fullName ?? ""} />
            </label>
            <label className={styles.field}>
                E-post
                <input name="email" type="email" required maxLength={250} autoComplete="email"
                       defaultValue={state.values?.email ?? ""} />
            </label>
            <label className={styles.field}>
                Passord
                <input name="password" type="password" required minLength={6} autoComplete="new-password" />
            </label>
            <p className={styles.hint}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
            <p className={styles.hint}>
                Ved å opprette konto godtar du <NextLink href="/vilkar">vilkårene</NextLink> og bekrefter at du
                har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={`${styles.primaryButton} ${styles.loginButton}`}>Opprett konto</SubmitButton>
        </form>
    );
}