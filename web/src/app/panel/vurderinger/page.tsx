import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelReviews } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import type { Review } from "@/lib/types";
import Stars from "@/components/Stars/Stars";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import { replyToReviewAction } from "./actions";
import ReplyText from "./ReplyText";
import { replyIcon } from "./replyLines";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Vurderinger",
};

type PanelReviewsPageProps = {
    searchParams: Promise<{ svart?: string; feil?: string; alle?: string }>;
};

const RECENT_DAYS = 30;

// Reviews written in the last RECENT_DAYS days
function recentReviews(reviews: Review[]) {
    const since = Date.now() - RECENT_DAYS * 24 * 60 * 60 * 1000;
    return reviews.filter((review) => new Date(review.createdUtc).getTime() >= since);
}

// "Vurderinger" (Ada 2026-11-01): the dark glass look of the other panel pages. The average big in Inter with
// big crystal stars beside it; every review a glass card. Lilac = waiting for the business's answer, mint = answered,
// blue = hidden by BESTI
export default async function PanelReviewsPage({ searchParams }: PanelReviewsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const data = await getPanelReviews(token);
    if (!data) {
        redirect("/logg-inn");
    }
    const { svart, feil, alle } = await searchParams;
    // The panel shows the last 30 days; older reviews stay on the business page but only open on request
    const showAll = alle === "1";
    const recent = recentReviews(data.reviews);
    const olderCount = data.reviews.length - recent.length;
    const shown = showAll ? data.reviews : recent;
    const waiting = recent.filter((review) => !review.businessReply && !review.hidden).length;

    return (
        <main className={panel.main}>
            <PanelNav title="Vurderinger"/>
            {svart && <p className={styles.notice} role="status">Svaret er lagret. Det vises på bedriftssiden.</p>}
            {feil && <p className={styles.error} role="alert">Svaret kunne ikke lagres. Prøv igjen.</p>}

            {/* Top: the average the customers see, and how many reviews wait for an answer */}
            <section className={styles.hero}>
                {data.average === null ? (
                    <p className={styles.heroEmpty}>Ingen vurderinger ennå.</p>
                ) : (
                    <div className={styles.score}>
                        <p className={styles.average}>{data.average.toString().replace(".", ",")}</p>
                        <div className={styles.scoreText}>
                            <Stars value={data.average}/>
                            <p className={styles.small}>
                                {data.count} {data.count === 1 ? "vurdering" : "vurderinger"} på bedriftssiden
                            </p>
                        </div>
                    </div>
                )}
                <div className={styles.tiles}>
                    <p className={styles.tile}>
                        <span className={styles.tileNumber}>{recent.length}</span>
                        <span className={styles.tileText}>nye siste {RECENT_DAYS} dager</span>
                    </p>
                    <p className={waiting > 0 ? `${styles.tile} ${styles.tileWaiting}` : styles.tile}>
                        <span className={styles.tileNumber}>{waiting}</span>
                        <span className={styles.tileText}>venter på svar</span>
                    </p>
                </div>
                <p className={styles.small}>
                    Svaret ditt vises under vurderingen på bedriftssiden. Vurderinger kan ikke slettes, men bryter en
                    vurdering reglene (hets, personopplysninger, falsk), skriv til post@bestiapp.no, så ser BESTI på den.
                </p>
            </section>

            <section className={styles.section}>
                <h2 className={styles.sectionTitle}>
                    {showAll ? "Alle vurderinger" : `Siste ${RECENT_DAYS} dager`}
                    <span className={styles.count}>{shown.length}</span>
                </h2>
                {shown.length === 0 ? (
                    <p className={styles.small}>
                        {data.reviews.length > 0 ? `Ingen nye vurderinger de siste ${RECENT_DAYS} dagene.` : "Når kundene skriver en vurdering, kommer den her."}
                    </p>
                ) : (
                    <ul className={styles.list}>
                        {shown.map((review) => (
                            <ReviewCard key={review.id} review={review} showAll={showAll}/>
                        ))}
                    </ul>
                )}
            </section>

            {showAll ? (
                <Link href="/panel/vurderinger" className={styles.pill}>Vis bare de siste {RECENT_DAYS} dagene</Link>
            ) : olderCount > 0 && (
                <Link href="/panel/vurderinger?alle=1" className={styles.pill}>Vis eldre vurderinger ({olderCount})</Link>
            )}
        </main>
    );
}

// One review: name, date and stars on top, the text, then the answer (lilac stamp while it waits)
function ReviewCard({ review, showAll }: { review: Review; showAll: boolean }) {
    const answered = review.businessReply !== null;
    const icon = replyIcon(review.businessReply);
    const cardClass = review.hidden ? styles.hidden : answered ? styles.card : `${styles.card} ${styles.cardWaiting}`;
    return (
        <li className={cardClass}>
            <div className={styles.cardHead}>
                <p className={styles.name}>{review.customerName}</p>
                <p className={styles.date}>{formatDate(review.createdUtc)}</p>
            </div>
            <Stars value={review.stars}/>
            {review.comment ? <p className={styles.comment}>{review.comment}</p> : <p className={styles.small}>Bare stjerner, ingen tekst.</p>}
            {review.photoUrl && (
                // eslint-disable-next-line @next/next/no-img-element -- uploaded photo from the API
                <img src={review.photoUrl} alt="Bilde fra kunden" className={styles.photo} loading="lazy"/>
            )}
            {review.helpful > 0 && (
                <p className={styles.helpful}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- tiny SVG icon */}
                    <img src="/ikoner/tommel.svg" alt="" width={20} height={20}/>
                    {review.helpful} syntes den var nyttig
                </p>
            )}
            {review.hidden && (
                <p className={styles.hiddenNote} role="note">
                    Skjult av BESTI: {review.hiddenReason}. Den vises ikke på siden og teller ikke i stjernene.
                </p>
            )}

            {answered && (
                <div className={styles.reply}>
                    <p className={styles.replyLabel}>Ditt svar</p>
                    <p className={styles.replyText}>
                        {review.businessReply}
                        {icon && (
                            // eslint-disable-next-line @next/next/no-img-element -- small SVG icon
                            <img src={`/ikoner/${icon}.svg`} alt="" aria-hidden="true" className={styles.chipIcon}/>
                        )}
                    </p>
                </div>
            )}
            {!answered && !review.hidden && <p className={styles.stamp}>Venter på svaret ditt</p>}

            {/* The answer field opens on a click, so the list stays short */}
            <details className={styles.more}>
                <summary className={answered ? styles.pill : `${styles.pill} ${styles.pillMint}`}>
                    {answered ? "Endre svar" : "Svar kunden"}
                </summary>
                <form action={replyToReviewAction} className={styles.sheet}>
                    <input type="hidden" name="reviewId" value={review.id}/>
                    {showAll && <input type="hidden" name="alle" value="1"/>}
                    <ReplyText answered={answered} initial={review.businessReply ?? ""}/>
                    <SubmitButton className={styles.saveButton} pendingText="Lagrer …">
                        {answered ? "Oppdater svar" : "Send svar"}
                    </SubmitButton>
                </form>
            </details>
        </li>
    );
}
