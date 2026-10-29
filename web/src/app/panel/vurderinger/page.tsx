import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelReviews } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import Stars from "@/components/Stars/Stars";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { replyToReviewAction } from "./actions";
import styles from "../tjenester/page.module.css";

export const metadata: Metadata = {
    title: "Vurderinger",
};

type PanelReviewsPageProps = {
    searchParams: Promise<{ svart?: string; feil?: string }>;
};

export default async function PanelReviewsPage({ searchParams }: PanelReviewsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const data = await getPanelReviews(token);
    if (!data) {
        redirect("/logg-inn");
    }
    const { svart, feil } = await searchParams;

    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Vurderinger</h1>
            <p className={styles.muted}>
                {data.average === null
                    ? "Ingen vurderinger ennå."
                    : `Snitt ${data.average.toString().replace(".", ",")} av 5 · ${data.count} ${data.count === 1 ? "vurdering" : "vurderinger"}`}
            </p>
            <p className={styles.muted}>
                Du kan svare på vurderinger. Svaret vises på bedriftssiden. Vurderinger kan ikke slettes – men
                bryter en vurdering reglene (hets, personopplysninger, falsk), skriv til post@bestiapp.no, så ser BESTI på den.
            </p>

            {svart && <p className={styles.success} role="status">Svaret er lagret.</p>}
            {feil && <p className={styles.error} role="alert">Svaret kunne ikke lagres. Prøv igjen.</p>}

            <ul className={styles.list}>
                {data.reviews.map((review) => (
                    <li key={review.id} className={styles.form}>
                        <p>
                            <Stars value={review.stars}/> <strong>{review.customerName}</strong> · {formatDate(review.createdUtc)}
                        </p>
                        {review.comment && <p>{review.comment}</p>}
                        {review.photoUrl && (
                            // eslint-disable-next-line @next/next/no-img-element -- uploaded photo from the API
                            <img src={review.photoUrl} alt="Bilde fra kunden" className={styles.reviewPhoto} loading="lazy"/>
                        )}
                        {review.hidden && (
                            <p className={styles.error} role="note">
                                Skjult av BESTI: {review.hiddenReason}. Den vises ikke på siden og teller ikke i stjernene.
                            </p>
                        )}
                        {review.helpful > 0 && <p className={styles.muted}>👍 {review.helpful} syntes den var nyttig</p>}
                        <form action={replyToReviewAction} className={styles.form}>
                            <input type="hidden" name="reviewId" value={review.id}/>
                            <label className={styles.field}>
                                {review.businessReply ? "Ditt svar (kan endres)" : "Svar til kunden"}
                                <textarea name="reply" required maxLength={500} rows={3}
                                          defaultValue={review.businessReply ?? ""}/>
                            </label>
                            <SubmitButton className={styles.primaryButton} pendingText="Lagrer …">
                                {review.businessReply ? "Oppdater svar" : "Send svar"}
                            </SubmitButton>
                        </form>
                    </li>
                ))}
            </ul>
        </main>
    );
}
