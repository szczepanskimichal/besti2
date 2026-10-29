import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminReviews } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import Stars from "@/components/Stars/Stars";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "Vurderinger" };

// The 50 newest reviews on all businesses. 1-2 stars are marked: a quick call to the business can save a customer
export default async function AdminReviewsPage() {
    const token = await getToken();
    const reviews = token ? await getAdminReviews(token) : null;
    if (!reviews) {
        notFound();
    }

    return (
        <>
            <h1 className={styles.title}>Siste vurderinger</h1>
            {reviews.length === 0 && <p className={styles.muted}>Ingen vurderinger ennå.</p>}
            <ul className={styles.list}>
                {reviews.map((review) => (
                    <li key={review.id} className={review.stars <= 2 ? `${styles.card} ${styles.lowRating}` : styles.card}>
                        <div className={styles.cardHeader}>
                            <Link href={`/admin/bedrifter/${review.businessId}`} className={styles.name}>{review.businessName}</Link>
                            <span className={styles.muted}>{formatDate(review.createdUtc)}</span>
                        </div>
                        <p>
                            <Stars value={review.stars}/> {review.stars} av 5 · {review.customerName}
                            {review.stars <= 2 && <strong className={styles.lowLabel}> Lav vurdering</strong>}
                        </p>
                        {review.comment && <p>{review.comment}</p>}
                        <p className={styles.muted}>
                            {review.businessReply ? `Svar fra bedriften: ${review.businessReply}` : "Bedriften har ikke svart."}
                        </p>
                    </li>
                ))}
            </ul>
        </>
    );
}
