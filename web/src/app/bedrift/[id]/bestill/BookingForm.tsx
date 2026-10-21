"use client";
import { useActionState } from "react";
import NextLink from "next/link";
import { createBookingAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type BookingFormProps = {
    businessId: string;
    serviceId: string;
    employeeId: string;
    startUtc: string;
    date: string;
    fullName: string | null;
    email: string;
};
// The booking form: on an error (e.g. the time was just taken) the phone, note and consent stay
export default function BookingForm({ businessId, serviceId, employeeId, startUtc, date, fullName, email }: BookingFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(createBookingAction, {});
    const value = (field: string) => state.values?.[field] ?? "";
    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <p className={styles.muted}>Bestiller som <strong>{fullName}</strong> ({email})</p>
            <input type="hidden" name="businessId" value={businessId}/>
            <input type="hidden" name="serviceId" value={serviceId}/>
            <input type="hidden" name="employeeId" value={employeeId}/>
            <input type="hidden" name="startUtc" value={startUtc}/>
            <input type="hidden" name="date" value={date}/>
            <label className={styles.field}>
                Telefon
                <input name="customerPhone" type="tel" inputMode="tel" required maxLength={20}
                       autoComplete="tel" pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567"
                       title="Bare tall, mellomrom og +, minst 8 sifre" defaultValue={value("customerPhone")}/>
            </label>
            <label className={styles.field}>
                Melding til bedriften (valgfritt)
                <textarea name="note" maxLength={500} rows={3}
                          placeholder="F.eks. «Jeg har langt hår»" defaultValue={value("note")}/>
            </label>
            <label className={styles.checkbox}>
                <input type="checkbox" name="marketingConsent" defaultChecked={value("marketingConsent") === "on"}/>
                Ja, jeg vil motta nyhetsbrev og tilbud på e-post
            </label>
            <p className={styles.muted}>
                Ved å bestille godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={styles.primaryButton} pendingText="Bestiller…">Bekreft bestilling</SubmitButton>
        </form>
    );
}