import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatTime } from "@/lib/format";
import { bookingsToReview, upcomingBookings } from "@/lib/minBesti";
import { answerMyNewTimeAction, cancelMyBookingAction } from "./actions";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
// Same cards, status colours and buttons as the business panel
import styles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Min Besti",
};

type MyBookingsPageProps = {
    searchParams: Promise<{ feil?: string; avbestilt?: string; svar?: string }>;
};

// Only the coming visits: past and cancelled ones are gone (past visits can still be reviewed for 30 days)
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
    const { feil, avbestilt, svar } = await searchParams;
    const upcoming = upcomingBookings(bookings);
    const toReview = bookingsToReview(bookings).length;
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Min Besti</h1>
            <MinBestiTabs/>
            {feil && (
                <p className={styles.error} role="alert">
                    {feil === "ny-tid"
                        ? "Svaret kunne ikke lagres. Timen kan allerede være endret eller ha startet."
                        : "Bestillingen kunne ikke avbestilles. Den kan allerede være avbestilt eller ha startet."}
                </p>
            )}
            {svar === "godtatt" && (
                <p className={styles.muted} role="status">Takk! Bedriften bekrefter den nye tiden.</p>
            )}
            {svar === "avslatt" && (
                <p className={styles.muted} role="status">Timen er avlyst, og bedriften har fått beskjed.</p>
            )}
            {avbestilt && <p className={styles.muted} role="status">Bestillingen er avbestilt.</p>}
            {toReview > 0 && (
                <p>
                    <Link href="/min-besti/vurderinger">
                        Du har {toReview === 1 ? "1 time" : `${toReview} timer`} du kan vurdere ⭐
                    </Link>
                </p>
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
                                    {booking.status === "VenterKunde"
                                        ? "Svar ønskes"
                                        : statusLabels[booking.status] ?? booking.status}
                                </span>
                            </div>
                            <p className={styles.service}>
                                {booking.serviceName} hos {booking.employeeName}
                            </p>
                            <p className={styles.customer}>
                                <Link href={`/bedrift/${booking.businessId}`}>{booking.businessName}</Link>
                                <br/>
                                {booking.businessAddress}
                            </p>
                            {/* The business moved the visit: the customer accepts or declines the new time */}
                            {booking.status === "VenterKunde" && (
                                <div className={styles.note}>
                                    <p>
                                        Bedriften ønsker å flytte timen
                                        {booking.previousStartUtc &&
                                            ` fra ${formatDate(booking.previousStartUtc)} kl. ${formatTime(booking.previousStartUtc)}`}.
                                        Passer den nye tiden?
                                    </p>
                                    <div className={styles.actions}>
                                        <form action={answerMyNewTimeAction}>
                                            <input type="hidden" name="bookingId" value={booking.id}/>
                                            <input type="hidden" name="svar" value="ja"/>
                                            <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Godta ny tid</SubmitButton>
                                        </form>
                                        <form action={answerMyNewTimeAction}>
                                            <input type="hidden" name="bookingId" value={booking.id}/>
                                            <input type="hidden" name="svar" value="nei"/>
                                            <SubmitButton className={styles.dangerButton} pendingText="Lagrer…">Passer ikke</SubmitButton>
                                        </form>
                                    </div>
                                </div>
                            )}
                            <p>
                                <a href={`/min-besti/kalender/${booking.id}`}>Legg til i kalender</a>
                            </p>
                            <details className={styles.actions}>
                                <summary>Avbestill</summary>
                                <p>Er du sikker? Tiden blir ledig for andre.</p>
                                <form action={cancelMyBookingAction}>
                                    <input type="hidden" name="bookingId" value={booking.id}/>
                                    <SubmitButton className={styles.dangerButton} pendingText="Avbestiller…">
                                        Ja, avbestill
                                    </SubmitButton>
                                </form>
                            </details>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
