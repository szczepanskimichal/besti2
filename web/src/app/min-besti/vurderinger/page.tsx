import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Fragment } from "react";
import { getMe, getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import type { CustomerBooking } from "@/lib/types";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { createReviewAction } from "../actions";
// Same cards and buttons as the rest of Min Besti
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Vurderinger",
};
// Visits that are over and were not cancelled; the ones waiting for a review first
function reviewableBookings(bookings: CustomerBooking[]) {
    const now = Date.now();
    return bookings
        .filter((b) => new Date(b.endUtc).getTime() <= now && b.status !== "Avlyst")
        .sort((a, b) => Number(a.isReviewed) - Number(b.isReviewed));
}
type ReviewsPageProps = {
    searchParams: Promise<{ takk?: string; feil?: string }>;
};
export default async function ReviewsPage({ searchParams }: ReviewsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/vurderinger");
    }
    const me = await getMe(token);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    const bookings = await getMyBookings(token);
    if (!bookings) {
        redirect("/logg-inn?neste=/min-besti/vurderinger");
    }
    const { takk, feil } = await searchParams;
    const visits = reviewableBookings(bookings);
    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Min Besti</h1>
            <MinBestiTabs/>
            {takk && <p className={styles.success} role="status">Takk for vurderingen! 💙</p>}
            {feil && (
                <p className={panelStyles.error} role="alert">
                    Vurderingen kunne ikke lagres. Du kan vurdere en time én gang, etter at den er over.
                </p>
            )}            <p className={panelStyles.muted}>
                Hvordan var timen? Bare fornavnet ditt vises på bedriftens side.
            </p>
            {visits.length === 0 ? (
                <p className={panelStyles.muted}>
                    Når du har vært på en time, kan du vurdere den her. <Link href="/">Finn en bedrift</Link>
                </p>
            ) : (
                <ul className={panelStyles.list}>
                    {visits.map((booking) => (
                        <li key={booking.id} id={`v-${booking.id}`} className={panelStyles.card}>
                            <p className={panelStyles.when}>{formatDate(booking.startUtc)}</p>
                            <p className={panelStyles.service}>
                                {booking.serviceName} hos{" "}
                                <Link href={`/bedrift/${booking.businessId}`}>{booking.businessName}</Link>
                            </p>
                            {booking.isReviewed ? (
                                <p className={panelStyles.muted}>✓ Du har vurdert denne timen. Takk!</p>
                            ) : (
                                <form action={createReviewAction} className={styles.form}>
                                    <input type="hidden" name="bookingId" value={booking.id}/>
                                    <p className={styles.label}>Hvor fornøyd var du?</p>
                                    {/* Stars 1..5 for keyboard and screen readers; CSS lights up the chosen one and those before it */}                                    <div className={styles.stars} role="radiogroup" aria-label="Antall stjerner">
                                        {[1, 2, 3, 4, 5].map((n) => (
                                            <Fragment key={n}>
                                                <input type="radio" id={`s${n}-${booking.id}`} name="stars" value={n} required/>
                                                <label htmlFor={`s${n}-${booking.id}`} title={`${n} av 5`}>
                                                    ★<span className={styles.srOnly}>{n} av 5 stjerner</span>
                                                </label>
                                            </Fragment>
                                        ))}
                                    </div>
                                    <label className={styles.label}>
                                        Kommentar (valgfritt)
                                        <textarea
                                            name="comment"
                                            maxLength={500}
                                            rows={3}
                                            className={styles.textarea}
                                            placeholder="Hva var bra? Hva kan bli bedre?"
                                        />
                                    </label>
                                    <SubmitButton className={panelStyles.primaryButton} pendingText="Sender…">
                                        Send vurdering
                                    </SubmitButton>
                                </form>
                            )}
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}