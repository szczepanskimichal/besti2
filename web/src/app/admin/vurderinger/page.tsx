import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminHiddenReviews, getAdminReasons, getAdminReviews } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import Stars from "@/components/Stars/Stars";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { reviewAction } from "../actions";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "Vurderinger" };

type AdminReviewsPageProps = {
    searchParams: Promise<{ vis?: string; endret?: string; feil?: string }>;
};

// The 50 newest reviews on all businesses (or only the hidden ones). 1-2 stars are marked: a quick call to the
// business can save a customer. "Skjul" takes a review off the page and out of the stars, with a fixed reason
export default async function AdminReviewsPage({ searchParams }: AdminReviewsPageProps) {
    const query = await searchParams;
    const onlyHidden = query.vis === "skjulte";
    const token = await getToken();
    const [reviews, reasons] = token
        ? await Promise.all([onlyHidden ? getAdminHiddenReviews(token) : getAdminReviews(token), getAdminReasons(token)])
        : [null, null];
    if (!reviews || !reasons) {
        notFound();
    }

    return (
        <>
            <h1 className={styles.title}>{onlyHidden ? "Skjulte vurderinger" : "Siste vurderinger"}</h1>
            <nav className={styles.filters} aria-label="Visning">
                <Link href="/admin/vurderinger" aria-current={onlyHidden ? undefined : "page"}
                      className={onlyHidden ? styles.filter : `${styles.filter} ${styles.filterActive}`}>Siste</Link>
                <Link href="/admin/vurderinger?vis=skjulte" aria-current={onlyHidden ? "page" : undefined}
                      className={onlyHidden ? `${styles.filter} ${styles.filterActive}` : styles.filter}>Skjulte</Link>
            </nav>
            {query.endret && <p className={styles.notice} role="status">Lagret. Kunden og bedriften har fått beskjed.</p>}
            {query.feil && <p className={styles.error} role="alert">{query.feil}</p>}
            <p className={styles.muted}>
                Skjul bare det som bryter reglene (hets, personopplysninger, falske vurderinger, reklame, upassende bilde).
                En dårlig, men ærlig vurdering skal stå – bedriften kan svare på den.
            </p>
            {reviews.length === 0 && <p className={styles.muted}>Ingen vurderinger her.</p>}
            <ul className={styles.list}>
                {reviews.map((review) => (
                    <li key={review.id} id={`v-${review.id}`}
                        className={review.stars <= 2 ? `${styles.card} ${styles.lowRating}` : styles.card}>
                        <div className={styles.cardHeader}>
                            <Link href={`/admin/bedrifter/${review.businessId}`} className={styles.name}>{review.businessName}</Link>
                            <span className={styles.muted}>{formatDate(review.createdUtc)}</span>
                        </div>
                        <p>
                            <Stars value={review.stars}/> {review.stars} av 5 · {review.customerName}
                            {review.stars <= 2 && <strong className={styles.lowLabel}> Lav vurdering</strong>}
                        </p>
                        {review.comment && <p>{review.comment}</p>}
                        {review.photoUrl && (
                            // eslint-disable-next-line @next/next/no-img-element -- uploaded photo from the API
                            <img src={review.photoUrl} alt="Bilde fra kunden" className={styles.photo} loading="lazy"/>
                        )}
                        <p className={styles.muted}>
                            {review.businessReply ? `Svar fra bedriften: ${review.businessReply}` : "Bedriften har ikke svart."}
                        </p>
                        {review.hiddenUtc ? (
                            <form action={reviewAction} className={styles.reasonForm}>
                                <span className={styles.hiddenLabel}>Skjult: {review.hiddenReason}</span>
                                <input type="hidden" name="id" value={review.id}/>
                                <input type="hidden" name="show" value="1"/>
                                <input type="hidden" name="back" value={onlyHidden ? "skjulte" : ""}/>
                                <SubmitButton className={styles.secondaryButton} pendingText="Viser …">Vis igjen</SubmitButton>
                            </form>
                        ) : (
                            <form action={reviewAction} className={styles.reasonForm}>
                                <input type="hidden" name="id" value={review.id}/>
                                <input type="hidden" name="back" value={onlyHidden ? "skjulte" : ""}/>
                                <label className={styles.srOnly} htmlFor={`grunn-${review.id}`}>Grunn</label>
                                <select id={`grunn-${review.id}`} name="reason" required defaultValue="" className={styles.select}>
                                    <option value="" disabled>Skjul fordi …</option>
                                    {reasons.review.map((reason) => <option key={reason} value={reason}>{reason}</option>)}
                                </select>
                                <SubmitButton className={styles.dangerButton} pendingText="Skjuler …">Skjul</SubmitButton>
                                {review.photoUrl && (
                                    <span className={styles.muted}>«Bildet passer ikke» sletter bildet for godt.</span>
                                )}
                            </form>
                        )}
                    </li>
                ))}
            </ul>
        </>
    );
}
