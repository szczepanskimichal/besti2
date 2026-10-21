"use client";
import { useActionState } from "react";
import { priceTypeOptions } from "@/lib/priceTypes";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type ServiceFormProps = {
    action: (previous: FormState, formData: FormData) => Promise<FormState>;
    // The saved service when editing; empty when adding a new one
    initial?: Record<string, string>;
    serviceId?: string;
    submitText: string;
};
// One form for "Legg til" and "Rediger": on an error the message shows here and everything typed stays
export default function ServiceForm({ action, initial = {}, serviceId, submitText }: ServiceFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(action, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? initial[field] ?? fallback;
    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            {serviceId && <input type="hidden" name="serviceId" value={serviceId} />}
            <label className={styles.field}>
                Navn
                <input name="name" required maxLength={200} defaultValue={value("name")} />
            </label>
            <label className={styles.field}>
                Beskrivelse (valgfritt)
                <input name="description" maxLength={500} defaultValue={value("description")} />
            </label>
            <label className={styles.field}>
                Hvor lenge varer timen? (minutter)
                <input name="durationMinutes" type="number" required min={5} max={480} step={5}
                       defaultValue={value("durationMinutes", "30")} />
            </label>
            <label className={styles.field}>
                Pristype
                {/* key: a select only takes a new defaultValue when it is created again */}
                <select key={value("priceType", "FastPris")} name="priceType" required
                        defaultValue={value("priceType", "FastPris")}>
                    {priceTypeOptions.map((option) => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                </select>
            </label>
            <label className={styles.field}>
                Pris i NOK (tom ved pris etter avtale)
                <input name="priceNok" type="number" min={0} step={1} defaultValue={value("priceNok")} />
            </label>
            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">
                {submitText}
            </SubmitButton>
        </form>
    );
}