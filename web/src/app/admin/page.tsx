import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminOverview } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { decideAction } from "./actions";
import BusinessRows from "./BusinessRows";
import styles from "./admin.module.css";

type AdminPageProps = {
    searchParams: Promise<{ feil?: string; avvist?: string }>;
};

// The day's work: who waits for approval, who needs a call, and how BESTI is doing
export default async function AdminPage({ searchParams }: AdminPageProps) {
    const token = await getToken();
    const overview = token ? await getAdminOverview(token) : null;
    if (!overview) {
        notFound();
    }
    const { feil, avvist } = await searchParams;
    const tiles = [
        { label: "Synlige bedrifter", value: overview.visibleBusinesses },
        { label: "Venter på godkjenning", value: overview.waitingForApproval },
        { label: "Ikke bekreftet e-post", value: overview.unconfirmedSignups },
        { label: "Kundekontoer", value: overview.customerAccounts },
        { label: "Bestillinger siste 7 dager", value: overview.bookingsLast7Days },
        { label: "Besøk siste 30 dager", value: overview.visitsLast30Days },
        { label: "Vurderinger siste 30 dager", value: overview.reviewsLast30Days },
        { label: "Interessert i Pro", value: overview.proInterested },
    ];

    return (
        <>
            <h1 className={styles.title}>Oversikt</h1>
            {feil && <p className={styles.error} role="alert">{feil}</p>}
            {avvist && <p className={styles.notice} role="status">Registreringen er avvist og slettet. Eieren har fått e-post.</p>}

            <section className={styles.tiles} aria-label="Tall">
                {tiles.map((tile) => (
                    <div key={tile.label} className={styles.tile}>
                        <p className={styles.tileValue}>{tile.value}</p>
                        <p className={styles.tileLabel}>{tile.label}</p>
                    </div>
                ))}
            </section>

            <h2 className={styles.heading}>Venter på godkjenning</h2>
            {overview.waiting.length === 0 ? (
                <p className={styles.muted}>Ingen akkurat nå. 🎉</p>
            ) : (
                <ul className={styles.list}>
                    {overview.waiting.map((business) => (
                        <li key={business.id} className={styles.card}>
                            <div className={styles.cardHeader}>
                                <Link href={`/admin/bedrifter/${business.id}`} className={styles.name}>{business.name}</Link>
                                <span className={styles.muted}>{formatDate(business.createdUtc)}</span>
                            </div>
                            <p className={styles.muted}>
                                {getCategoryLabel(business.category)} · {business.city} · {business.ownerEmail} · {business.phoneNumber}
                            </p>
                            <p className={styles.muted}>
                                Sjekk at personen representerer bedriften (ring nummeret i Brønnøysund) før du godkjenner.
                            </p>
                            <div className={styles.actions}>
                                <form action={decideAction}>
                                    <input type="hidden" name="id" value={business.id}/>
                                    <input type="hidden" name="decision" value="godkjenn"/>
                                    <SubmitButton className={styles.primaryButton} pendingText="Godkjenner …">Godkjenn</SubmitButton>
                                </form>
                                <Link href={`/admin/bedrifter/${business.id}?avvis=1`} className={styles.dangerButton}>Avvis …</Link>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            <h2 className={styles.heading}>Trenger hjelp</h2>
            <p className={styles.muted}>
                Synlige bedrifter som mangler noe i «Kom i gang», eller ikke har fått bestillinger på 30 dager.
                En telefon herfra gjør ofte hele forskjellen.
            </p>
            {overview.needHelp.length === 0
                ? <p className={styles.muted}>Alle ser bra ut.</p>
                : <BusinessRows rows={overview.needHelp}/>}
        </>
    );
}
