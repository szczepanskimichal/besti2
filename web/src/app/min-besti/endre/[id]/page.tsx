import type { Metadata } from "next";
import NextLink from "next/link";
import { redirect } from "next/navigation";
import { getAvailability, getBusiness, getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime, isIsoDate, todayInOslo } from "@/lib/format";
import type { CustomerBooking } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { changeMyBookingTimeAction } from "../../actions";
// The same time buttons as when booking
import styles from "../../../bedrift/[id]/bestill/page.module.css";

export const metadata: Metadata = {
    title: "Endre tid",
};

type ChangeTimePageProps = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ ansatt?: string; dato?: string; tid?: string; feil?: string }>;
};

// "Endre tid": the customer picks another free time for the same service; the old time is freed at once
export default async function ChangeTimePage({ params, searchParams }: ChangeTimePageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const { id } = await params;
    const { ansatt, dato, tid, feil } = await searchParams;

    const bookings = await getMyBookings(token);
    if (!bookings) {
        redirect("/logg-inn?neste=/min-besti");
    }
    // Only an own visit that has not started and is not cancelled
    const booking = bookings.find((b) => b.id === id);
    if (!booking || !canChange(booking)) {
        redirect("/min-besti");
    }

    const business = await getBusiness(booking.businessId);
    if (!business) {
        redirect("/min-besti");
    }
    const employeeId = business.employees.some((e) => e.id === ansatt) ? ansatt! : booking.employeeId;
    const date = isIsoDate(dato) ? dato : osloDate(booking.startUtc);
    const slots = await getAvailability(booking.serviceId, employeeId, date);
    const chosenTime = tid && slots?.includes(tid) ? tid : undefined;
    const base = `/min-besti/endre/${booking.id}?ansatt=${employeeId}&dato=${date}`;

    return (
        <main className={styles.main}>
            <NextLink href="/min-besti" className={styles.back}>← Tilbake til Min Besti</NextLink>

            <h1 className={styles.title}>Endre tid</h1>
            <p className={styles.muted}>
                {booking.serviceName} hos {booking.businessName}. Nå: {formatDate(booking.startUtc)} kl.{" "}
                {formatTime(booking.startUtc)} hos {booking.employeeName}.
            </p>

            {feil && (
                <p className={styles.error} role="alert">Tiden er ikke lenger ledig. Velg en annen tid.</p>
            )}

            <form method="get" className={styles.filters}>
                <label className={styles.field}>
                    Ansatt
                    <select name="ansatt" defaultValue={employeeId}>
                        {business.employees.map((employee) => (
                            <option key={employee.id} value={employee.id}>
                                {employee.name} {employee.lastName}
                            </option>
                        ))}
                    </select>
                </label>
                <label className={styles.field}>
                    Dato
                    <input type="date" name="dato" defaultValue={date} min={todayInOslo()}/>
                </label>
                <button type="submit" className={styles.secondaryButton}>Vis ledige tider</button>
            </form>

            <h2 className={styles.sectionTitle}>Ledige tider</h2>
            {!slots || slots.length === 0 ? (
                <p className={styles.muted}>Ingen ledige tider denne dagen. Prøv en annen dato.</p>
            ) : (
                <ul className={styles.slots}>
                    {slots.map((slot) => (
                        <li key={slot}>
                            <NextLink href={`${base}&tid=${encodeURIComponent(slot)}`}
                                      className={slot === chosenTime ? `${styles.slot} ${styles.slotSelected}` : styles.slot}
                                      aria-current={slot === chosenTime ? "true" : undefined}>
                                {formatTime(slot)}
                            </NextLink>
                        </li>
                    ))}
                </ul>
            )}
            {tid && !chosenTime && (
                <p className={styles.error} role="alert">Den valgte tiden er ikke lenger ledig. Velg en annen tid.</p>
            )}

            {chosenTime && (
                <section>
                    <h2 className={styles.sectionTitle}>Ny tid</h2>
                    <p className={styles.muted}>
                        {formatDate(chosenTime)} kl. {formatTime(chosenTime)}. Den gamle tiden blir ledig, og
                        bedriften bekrefter den nye.
                    </p>
                    <form action={changeMyBookingTimeAction} className={styles.form}>
                        <input type="hidden" name="bookingId" value={booking.id}/>
                        <input type="hidden" name="employeeId" value={employeeId}/>
                        <input type="hidden" name="date" value={date}/>
                        <input type="hidden" name="startUtc" value={chosenTime}/>
                        <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Bekreft ny tid</SubmitButton>
                    </form>
                </section>
            )}
        </main>
    );
}

// The API refuses cancelled visits and visits that have started
function canChange(booking: CustomerBooking): boolean {
    return booking.status !== "Avlyst" && new Date(booking.startUtc).getTime() > Date.now();
}

// "2026-10-26T08:30:00Z" -> "2026-10-26" (the day in Oslo)
function osloDate(utc: string): string {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Oslo" }).format(new Date(utc));
}