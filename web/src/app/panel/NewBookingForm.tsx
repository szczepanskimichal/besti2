"use client";
import { useActionState } from "react";
import Link from "next/link";
import { createManualBookingAction } from "./actions";
import type { FormState } from "@/lib/formState";
import type { Employee, Service } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import formStyles from "./tjenester/page.module.css";
import calendar from "./calendar.module.css";

type NewBookingFormProps = {
    services: Service[];
    employees: Employee[];
    date: string;
    time: string;
    employeeId?: string;
    week: boolean;
    closeHref: string;
};

// "+ Ny bestilling": the business adds a visit itself (phone call, walk-in); confirmed at once
export default function NewBookingForm({ services, employees, date, time, employeeId, week, closeHref }: NewBookingFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(createManualBookingAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;

    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="new-booking-title">
                <div className={calendar.dialogHead}>
                    <h2 id="new-booking-title" className={calendar.dialogTitle}>Ny bestilling</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <form action={formAction} className={calendar.dialogForm}>
                    {state.error && (
                        <p className={formStyles.error} role="alert">
                            {state.error}
                        </p>
                    )}
                    {week && <input type="hidden" name="visning" value="uke"/>}
                    <label className={formStyles.field}>
                        Tjeneste
                        {/* key: a select only takes a new defaultValue when it is created again */}
                        <select key={value("serviceId")} name="serviceId" required defaultValue={value("serviceId", services[0]?.id)}>
                            {services.map((s) => (
                                <option key={s.id} value={s.id}>{s.name} ({s.durationMinutes} min)</option>
                            ))}
                        </select>
                    </label>
                    <label className={formStyles.field}>
                        Ansatt
                        <select key={value("employeeId")} name="employeeId" required
                                defaultValue={value("employeeId", employeeId ?? employees[0]?.id)}>
                            {employees.map((e) => (
                                <option key={e.id} value={e.id}>{e.name} {e.lastName}</option>
                            ))}
                        </select>
                    </label>
                    <div className={calendar.formRow}>
                        <label className={formStyles.field}>
                            Dato
                            <input type="date" name="date" required defaultValue={value("date", date)}/>
                        </label>
                        <label className={formStyles.field}>
                            Klokkeslett
                            <input type="time" name="time" required step={300} defaultValue={value("time", time)}/>
                        </label>
                    </div>
                    <label className={formStyles.field}>
                        Kundens navn
                        <input name="customerName" required maxLength={100} autoComplete="off" defaultValue={value("customerName")}/>
                    </label>
                    <label className={formStyles.field}>
                        Telefon
                        <input name="customerPhone" type="tel" inputMode="tel" required maxLength={20}
                               pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567" autoComplete="off"
                               defaultValue={value("customerPhone")}/>
                    </label>
                    <label className={formStyles.field}>
                        E-post (valgfritt)
                        <input name="customerEmail" type="email" maxLength={250} autoComplete="off"
                               defaultValue={value("customerEmail")}/>
                    </label>
                    <label className={formStyles.field}>
                        Notat (valgfritt)
                        <textarea name="note" maxLength={500} rows={2} defaultValue={value("note")}/>
                    </label>
                    <label className={calendar.checkbox}>
                        <input type="checkbox" name="sendConfirmation" defaultChecked={value("sendConfirmation") === "on"}/>
                        Send bekreftelse til kunden på e-post
                    </label>
                    <SubmitButton className={formStyles.primaryButton} pendingText="Lagrer…">Lagre bestilling</SubmitButton>
                </form>
            </section>
        </div>
    );
}
