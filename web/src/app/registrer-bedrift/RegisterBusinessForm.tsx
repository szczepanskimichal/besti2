"use client";
import { useActionState } from "react";
import NextLink from "next/link";
import { categoryOptionGroups } from "@/lib/categories";
import { registerAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

// The business sign-up form: on an error every field stays filled in, only the password is typed again
export default function RegisterBusinessForm() {
    const [state, formAction] = useActionState<FormState, FormData>(registerAction, {});
    const value = (field: string) => state.values?.[field] ?? "";

    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <label className={styles.field}>
                Bedriftsnavn
                <input name="businessName" required maxLength={200} autoComplete="organization"
                       defaultValue={value("businessName")} />
            </label>
            <label className={styles.field}>
                Organisasjonsnummer (9 siffer)
                <input name="orgNumber" required inputMode="numeric" pattern="[0-9]{9}" maxLength={9}
                       defaultValue={value("orgNumber")} />
            </label>
            <label className={styles.field}>
                Kategori
                {/* key: a select only takes a new defaultValue when it is created again */}
                <select key={value("category")} name="category" required defaultValue={value("category")}>
                    <option value="" disabled>Velg kategori</option>
                    {categoryOptionGroups.map((group) => (
                        <optgroup key={group.label} label={group.label}>
                            {group.options.map((option) => (
                                <option key={option.value} value={option.value}>{option.label}</option>
                            ))}
                        </optgroup>
                    ))}
                </select>
            </label>
            <label className={styles.field}>
                Adresse
                <input name="address" required autoComplete="street-address" defaultValue={value("address")} />
            </label>
            <label className={styles.field}>
                Postnummer
                <input name="postalCode" required inputMode="numeric" pattern="[0-9]{4}" maxLength={4}
                       autoComplete="postal-code" defaultValue={value("postalCode")} />
            </label>
            <label className={styles.field}>
                By
                <input name="city" required autoComplete="address-level2" defaultValue={value("city")} />
            </label>
            <label className={styles.field}>
                Telefon
                <input name="phone" type="tel" required maxLength={20} autoComplete="tel" defaultValue={value("phone")} />
            </label>
            <label className={styles.field}>
                E-post (brukes til innlogging)
                <input name="email" type="email" required maxLength={250} autoComplete="email"
                       defaultValue={value("email")} />
            </label>
            <label className={styles.field}>
                Passord
                <input name="password" type="password" required minLength={6} autoComplete="new-password" />
            </label>
            <p className={styles.hint}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
            <p className={styles.hint}>
                Ved å registrere bedriften godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={styles.primaryButton} pendingText="Registrerer…">
                Registrer bedrift
            </SubmitButton>
        </form>
    );
}
