"use client";
import { useActionState } from "react";
import NextLink from "next/link";
import { acceptInviteAction } from "../actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

// "Jeg blir med!": the friend's phone (from Min profil) and an optional message; name and e-mail come from the account
export default function AcceptForm({ token, fullName, phone }: { token: string; fullName: string | null; phone: string | null }) {
    const [state, formAction] = useActionState<FormState, FormData>(acceptInviteAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;

    return (
        <form action={formAction} className={styles.form}>
            {state.error && <p className={styles.error} role="alert">{state.error}</p>}
            <input type="hidden" name="token" value={token}/>
            <p className={styles.muted}>Du blir med som <strong>{fullName}</strong></p>
            <label className={styles.field}>
                Din telefon
                <input name="phone" type="tel" inputMode="tel" required maxLength={20}
                       autoComplete="tel" pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567"
                       title="Bare tall, mellomrom og +, minst 8 sifre"
                       defaultValue={value("phone", phone ?? "")}/>
            </label>
            <label className={styles.field}>
                Melding til bedriften (valgfritt)
                <textarea name="note" maxLength={500} rows={2} defaultValue={value("note")}/>
            </label>
            <p className={styles.muted}>
                Ved å bli med godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={styles.primaryButton} pendingText="Bestiller for dere…">
                Jeg blir med! <span aria-hidden="true">👯</span>
            </SubmitButton>
        </form>
    );
}
