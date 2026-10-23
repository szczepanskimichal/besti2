import type { Metadata } from "next";
import Link from "next/link";
import { getBusinessApproval } from "@/lib/api";
import { getCategoryLabel } from "@/lib/categories";
import { privatePage } from "@/lib/site";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { approveBusinessAction } from "./actions";
import styles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Godkjenn bedrift",
    robots: privatePage,
};

type ApprovalPageProps = {
    searchParams: Promise<{ token?: string; svar?: string }>;
};

// For BESTI: the link in the "Ny bedrift venter på godkjenning" mail. The link only shows the business;
// approving is a button, so a mail program that opens links on its own cannot approve it
export default async function ApprovalPage({ searchParams }: ApprovalPageProps) {
    const { token, svar } = await searchParams;

    if (svar) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Godkjenn bedrift</h1>
                <p role="status">
                    {svar === "godkjent"
                        ? "Bedriften er godkjent og synlig for kundene. Eieren har fått beskjed på e-post."
                        : "Kunne ikke godkjenne. Lenken kan være brukt allerede."}
                </p>
                <p><Link href="/">Til forsiden</Link></p>
            </main>
        );
    }

    const business = token ? await getBusinessApproval(token) : null;
    if (!token || !business) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Godkjenn bedrift</h1>
                <p>Lenken er ikke gyldig. Bedriften er kanskje godkjent allerede.</p>
            </main>
        );
    }

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Godkjenn bedrift</h1>
            <div className={styles.card}>
                <p className={styles.when}>{business.name}</p>
                <p className={styles.service}>
                    Org.nr. {business.orgNumber} · {getCategoryLabel(business.category)}
                </p>
                <p className={styles.muted}>
                    {business.address}, {business.postalCode} {business.city}<br/>
                    {business.email} · {business.phoneNumber}
                </p>
                <p>
                    <a href={`https://virksomhet.brreg.no/nb/oppslag/enheter/${business.orgNumber}`}
                       target="_blank" rel="noopener noreferrer">Se bedriften i Brønnøysundregistrene</a>
                </p>
                <form action={approveBusinessAction}>
                    <input type="hidden" name="token" value={token}/>
                    <SubmitButton className={styles.primaryButton} pendingText="Godkjenner…">Godkjenn</SubmitButton>
                </form>
            </div>
        </main>
    );
}
