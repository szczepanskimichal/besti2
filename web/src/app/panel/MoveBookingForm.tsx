"use client";
import { useActionState } from "react";
import Link from "next/link";
import { moveBookingAction } from "./actions";
import type { FormState } from "@/lib/formState";
import type { Employee, PanelBooking } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import formStyles from "./tjenester/page.module.css";
import calendar from "./calendar.module.css";

type MoveBookingFormProps = {
    booking: PanelBooking;
    employees: Employee[];
    date: string; // the visit's day and time in Oslo, filled in
    time: string;
    view?: string;
    closeHref: string;
};

// "Flytt": the same visit at another time and/or with another employee
export default function MoveBookingForm({ booking, employees, date, time, view, closeHref }: MoveBookingFormProps) {
    const [state, formAction] = useActionState<FormState, FormData>(moveBookingAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;

    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="move-title">
                <div className={calendar.dialogHead}>
                    <h2 id="move-title" className={calendar.dialogTitle}>Flytt {booking.customerName}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogHint}>{booking.serviceName}</p>
                <form action={formAction} className={calendar.dialogForm}>
                    {state.error && (
                        <p className={formStyles.error} role="alert">
                            {state.error}
                        </p>
                    )}
                    <input type="hidden" name="bookingId" value={booking.id}/>
                    {view && <input type="hidden" name="visning" value={view}/>}
                    <label className={formStyles.field}>
                        Ansatt
                        {/* key: a select only takes a new defaultValue when it is created again */}
                        <select key={value("employeeId")} name="employeeId" required
                                defaultValue={value("employeeId", booking.employeeId)}>
                            {employees.map((e) => (
                                <option key={e.id} value={e.id}>{e.name} {e.lastName}</option>
                            ))}
                        </select>
                    </label>
                    <div className={calendar.formRow}>
                        <label className={formStyles.field}>
                            Ny dato
                            <input type="date" name="date" required defaultValue={value("date", date)}/>
                        </label>
                        <label className={formStyles.field}>
                            Ny tid
                            <input type="time" name="time" required step={300} defaultValue={value("time", time)}/>
                        </label>
                    </div>
                    {booking.customerEmail ? (
                        <fieldset className={calendar.choice}>
                            <legend>Kunden</legend>
                            <label className={calendar.checkbox}>
                                <input type="radio" name="svar" value="spor"
                                       defaultChecked={value("svar", "spor") === "spor"}/>
                                Be kunden godta den nye tiden (e-post med lenke)
                            </label>
                            <label className={calendar.checkbox}>
                                <input type="radio" name="svar" value="ja" defaultChecked={value("svar") === "ja"}/>
                                Kunden har allerede sagt ja (f.eks. på telefon)
                            </label>
                        </fieldset>
                    ) : (
                        <p className={calendar.dialogHint}>Kunden har ingen e-post – husk å gi beskjed på telefon.</p>
                    )}
                    <SubmitButton className={formStyles.primaryButton} pendingText="Flytter…">Flytt timen</SubmitButton>
                </form>
            </section>
        </div>
    );
}
