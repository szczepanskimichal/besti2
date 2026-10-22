import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatTime } from "@/lib/format";
import type { CustomerBooking } from "@/lib/types";
import { cancelMyBookingAction } from "./actions";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
// Same cards, status colours and buttons as the business panel
import styles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Min Besti",
};

// Upcoming = has not started yet; the API already sorts newest first
function splitBookings(bookings: CustomerBooking[]) {
    const now = Date.now();
    const upcoming = bookings
        .filter((b) => new Date(b.startUtc).getTime() > now)
        .reverse(); // soonest first
    const past = bookings.filter((b) => new Date(b.startUtc).getTime() <= now);
    return { upcoming, past };
}

type MyBookingsPageProps = {
    searchParams: Promise<{ feil?: string; avbestilt?: string; endret?: string }>;
};
export default async function MyBookingsPage({ searchParams }: MyBookingsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    // Both at the same time instead of one after the other
    const [me, bookings] = await Promise.all([getMe(token), getMyBookings(token)]);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    if (!bookings) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const { feil, avbestilt, endret } = await searchParams;
    const { upcoming, past } = splitBookings(bookings);
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Min Besti</h1>
            <MinBestiTabs/>
            {feil && (
                <p className={styles.error} role="alert">
                    Bestillingen kunne ikke avbestilles. Den kan allerede være avbestilt eller ha startet.
                </p>
            )}
            {avbestilt && <p className={styles.muted} role="status">Bestillingen er avbestilt.</p>}
            {endret && (
                <p className={styles.muted} role="status">Tiden er endret. Bedriften bekrefter den nye tiden.</p>
            )}
            <h2>Kommende</h2>
            {upcoming.length === 0 ? (
                <p className={styles.muted}>
                    Du har ingen kommende bestillinger. <Link href="/">Finn en bedrift</Link>
                </p>
            ) : (
                <ul className={styles.list}>
                    {upcoming.map((booking) => (
                        <li key={booking.id} className={styles.card}>
                            <div className={styles.cardHeader}>
                                <p className={styles.when}>
                                    {formatDate(booking.startUtc)}, {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                </p>
                                <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
{statusLabels[booking.status] ?? booking.status}
</span>
                            </div>
                            <p className={styles.service}>
                                {booking.serviceName} hos {booking.employeeName}
                            </p>
                            <p className={styles.customer}>
                                <Link href={`/bedrift/${booking.businessId}`}>{booking.businessName}</Link>
                                <br />
                                {booking.businessAddress}
                            </p>
                            {booking.status !== "Avlyst" && (
                                <p>
                                    <Link href={`/min-besti/endre/${booking.id}`}>Endre tid</Link>
                                    {" · "}
                                    <a href={`/min-besti/kalender/${booking.id}`}>Legg til i kalender</a>
                                </p>
                            )}
                            {/* Cancelled by the business: straight back to its booking page */}
                            {booking.status === "Avlyst" && (
                                <p>
                                    <Link href={`/bedrift/${booking.businessId}/bestill?tjeneste=${booking.serviceId}&ansatt=${booking.employeeId}`}>
                                        Bestill ny tid →
                                    </Link>
                                </p>
                            )}
                            {booking.status !== "Avlyst" && (
                                <details className={styles.actions}>
                                    <summary>Avbestill</summary>
                                    <p>Er du sikker? Tiden blir ledig for andre.</p>
                                    <form action={cancelMyBookingAction}>
                                        <input type="hidden" name="bookingId" value={booking.id} />
                                        <SubmitButton className={styles.dangerButton} pendingText="Avbestiller…">
                                            Ja, avbestill
                                        </SubmitButton>
                                    </form>
                                </details>
                            )}
                        </li>
                    ))}
                </ul>
            )}
            {past.length > 0 && (
                <>
                    <h2>Tidligere</h2>
                    <ul className={styles.list}>
                        {past.map((booking) => (
                            <li key={booking.id} className={styles.card}>
                                <div className={styles.cardHeader}>
                                    <p className={styles.when}>
                                        {formatDate(booking.startUtc)}, {formatTime(booking.startUtc)}
                                    </p>
                                    <span className={`${styles.status} ${styles[booking.status] ?? ""}`}>
{statusLabels[booking.status] ?? booking.status}
</span>
                                </div>
                                <p className={styles.service}>
                                    {booking.serviceName} hos {booking.businessName}
                                </p>
                                <p>
                                    <Link href={`/bedrift/${booking.businessId}/bestill?tjeneste=${booking.serviceId}&ansatt=${booking.employeeId}`}>
                                        Bestill igjen →
                                    </Link>
                                </p>
                                {booking.status !== "Avlyst" && !booking.isReviewed && (
                                    <p>
                                        <Link href={`/min-besti/vurderinger#v-${booking.id}`}>Gi vurdering ⭐</Link>
                                    </p>
                                )}
                            </li>
                        ))}
                    </ul>
                </>
            )}
        </main>
    );
}
