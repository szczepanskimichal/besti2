"use client";
import { useActionState } from "react";
import { updateProfileAction } from "./actions";
import type { FormState } from "@/lib/formState";
import { FREE_CANCEL_OPTIONS } from "@/lib/freeCancel";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../tjenester/page.module.css";

type ProfileFormProps = {
    // The saved profile
    initial: Record<string, string>;
};

// Description and contact info: on an error the message shows here and everything typed stays
export default function ProfileForm({ initial }: ProfileFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(updateProfileAction, {});
    const value = (field: string) => state.values?.[field] ?? initial[field] ?? "";

    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <label className={styles.field}>
                Om oss (valgfritt)
                <textarea name="description" maxLength={1000} rows={5} defaultValue={value("description")} />
            </label>
            <label className={styles.field}>
                Telefon
                <input name="phone" type="tel" required maxLength={20} defaultValue={value("phone")} />
            </label>
            <label className={styles.field}>
                E-post til kunder (valgfritt)
                <input name="contactEmail" type="email" maxLength={250} defaultValue={value("contactEmail")} />
            </label>
            <label className={styles.field}>
                Adresse
                <input name="address" required defaultValue={value("address")} />
            </label>
            <label className={styles.field}>
                Postnummer
                <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4}
                       defaultValue={value("postalCode")} />
            </label>
            <label className={styles.field}>
                By
                <input name="city" required maxLength={200} defaultValue={value("city")} />
            </label>
            <label className={styles.field}>
                Gratis avbestilling inntil
                <select name="freeCancelHours" defaultValue={value("freeCancelHours")}>
                    {FREE_CANCEL_OPTIONS.map((hours) => (
                        <option key={hours} value={hours}>
                            {hours === 0 ? "Når som helst før timen" : `${hours} timer før timen`}
                        </option>
                    ))}
                </select>
            </label>
            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">
                Lagre profil
            </SubmitButton>
        </form>
    );
}
