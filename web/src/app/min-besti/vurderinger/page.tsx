import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import { bookingsToReview } from "@/lib/minBesti";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import ReviewForm from "./ReviewForm";
// Same cards and buttons as the rest of Min Besti
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Vurderinger",
};
type ReviewsPageProps = {
    searchParams: Promise<{ takk?: string; bilde?: string }>;
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
    const { takk, bilde } = await searchParams;
    // Only visits still waiting for a review (at most 30 days old); reviewed ones disappear
    const visits = bookingsToReview(bookings);
    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Min Besti</h1>
            <MinBestiTabs/>
            {takk && <p className={styles.success} role="status">Takk for vurderingen! 💙</p>}
            {bilde === "feil" && (
                <p className={panelStyles.error} role="alert">Vurderingen er lagret, men bildet kunne ikke lastes opp.</p>
            )}
            <p className={panelStyles.muted}>
                Hvordan var timen? Bare fornavnet ditt vises på bedriftens side. Du kan vurdere en time i 30 dager.
            </p>
            {visits.length === 0 ? (
                <p className={panelStyles.muted}>
                    Ingen timer å vurdere akkurat nå. <Link href="/">Finn en bedrift</Link>
                </p>
            ) : (
                <ul className={panelStyles.list}>
                    {visits.map((booking) => (
                        <li key={booking.id} id={`v-${booking.id}`} className={panelStyles.card}>
                            <p className={panelStyles.when}>{formatDate(booking.startUtc)}</p>
                            <p className={panelStyles.service}>
                                {booking.serviceName} hos{" "}
                                <Link href={`/${booking.businessSlug}`}>{booking.businessName}</Link>
                            </p>
                            <ReviewForm bookingId={booking.id}/>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
