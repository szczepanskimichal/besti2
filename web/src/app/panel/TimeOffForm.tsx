"use client";
import { useActionState, useState } from "react";
import Link from "next/link";
import { createTimeOffAction } from "./actions";
import type { FormState } from "@/lib/formState";
import type { Employee } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import formStyles from "./tjenester/page.module.css";
import calendar from "./calendar.module.css";

type TimeOffFormProps = {
    employees: Employee[];
    date: string;
    employeeId?: string;
    view?: string;
    closeHref: string;
};

// "Blokker tid": holiday, doctor, closed day; for one employee or the whole business
export default function TimeOffForm({ employees, date, employeeId, view, closeHref }: TimeOffFormProps){
    const [state, formAction] = useActionState<FormState, FormData>(createTimeOffAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;
    // A whole day needs no times
    const [allDay, setAllDay] = useState(value("allDay") === "on");

    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="time-off-title">
                <div className={calendar.dialogHead}>
                    <h2 id="time-off-title" className={calendar.dialogTitle}>Blokker tid</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogHint}>Kundene kan ikke bestille time i denne perioden.</p>
                <form action={formAction} className={calendar.dialogForm}>
                    {state.error && (
                        <p className={formStyles.error} role="alert">
                            {state.error}
                        </p>
                    )}
                    {view && <input type="hidden" name="visning" value={view}/>}
                    <label className={formStyles.field}>
                        Hvem
                        {/* key: a select only takes a new defaultValue when it is created again */}
                        <select key={value("employeeId")} name="employeeId"
                                defaultValue={value("employeeId", employeeId ?? employees[0]?.id)}>
                            {employees.map((e) => (
                                <option key={e.id} value={e.id}>{e.name} {e.lastName}</option>
                            ))}
                            <option value="">Hele bedriften (stengt)</option>
                        </select>
                    </label>
                    <label className={calendar.checkbox}>
                        <input type="checkbox" name="allDay" checked={allDay} onChange={(e) => setAllDay(e.target.checked)}/>
                        Hele dagen
                    </label>
                    <div className={calendar.formRow}>
                        <label className={formStyles.field}>
                            Fra dato
                            <input type="date" name="fromDate" required defaultValue={value("fromDate", date)}/>
                        </label>
                        {!allDay && (
                            <label className={formStyles.field}>
                                Fra kl.
                                <input type="time" name="fromTime" required step={300} defaultValue={value("fromTime", "09:00")}/>
                            </label>
                        )}
                    </div>
                    <div className={calendar.formRow}>
                        <label className={formStyles.field}>
                            Til dato
                            <input type="date" name="toDate" required defaultValue={value("toDate", date)}/>
                        </label>
                        {!allDay && (
                            <label className={formStyles.field}>
                                Til kl.
                                <input type="time" name="toTime" required step={300} defaultValue={value("toTime", "10:00")}/>
                            </label>
                        )}
                    </div>
                    <label className={formStyles.field}>
                        Beskrivelse (valgfritt, bare dere ser den)
                        <input name="reason" maxLength={100} placeholder="F.eks. Ferie, Lege, Kurs" autoComplete="off"
                               defaultValue={value("reason")}/>
                    </label>
                    <SubmitButton className={formStyles.primaryButton} pendingText="Lagrer…">Blokker tid</SubmitButton>
                </form>
            </section>
        </div>
    );
}
