import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyFavorites, getMyLoyalty } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate, formatTime } from "@/lib/format";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import StampCard from "@/components/StampCard/StampCard";
import Stars from "@/components/Stars/Stars";
import { toggleFavoriteAction } from "./actions";
import panel from "../../panel/page.module.css";
import styles from "./page.module.css";
import EmptyState from "@/components/EmptyState/EmptyState";

export const metadata: Metadata = { title: "Favoritter" };

type FavoritesPageProps = {
    searchParams: Promise<{ favoritt?: string }>;
};

// "Favoritter": the customer's saved businesses with "Bestill igjen" (same service and employee as last time)
export default async function FavoritesPage({ searchParams }: FavoritesPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/favoritter");
    }
    const [me, favorites, cards] = await Promise.all([getMe(token), getMyFavorites(token), getMyLoyalty(token)]);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    if (!favorites) {
        redirect("/logg-inn?neste=/min-besti/favoritter");
    }
    const { favoritt } = await searchParams;

    return (
        <main className={panel.main}>
            <h1 className={panel.title}>Min Besti</h1>
            <MinBestiTabs/>
            {favoritt === "feil" && <p className={panel.error} role="alert">Det gikk ikke. Prøv igjen.</p>}

            {/* "Stempelkort": only businesses with a card where the customer has been */}
            {cards.length > 0 && (
                <section aria-labelledby="stempelkort" className={styles.section}>
                    <h2 id="stempelkort" className={styles.heading}>Stempelkort <span aria-hidden="true">🎟️</span></h2>
                    <ul className={styles.grid}>
                        {cards.map((card) => (
                            <li key={card.businessId} className={card.rewardReady ? `${panel.card} ${styles.ready}` : panel.card}>
                                <h3 className={styles.name}><Link href={`/${card.businessSlug}`}>{card.businessName}</Link></h3>
                                <StampCard stamps={card.stamps} visits={card.visits}/>
                                <p className={panel.muted}>
                                    {card.rewardReady
                                        ? <>Fullt kort! Si fra ved neste besøk, så får du <strong>{card.reward}</strong>.</>
                                        : <>{card.visits - card.stamps} besøk igjen til <strong>{card.reward}</strong>.</>}
                                </p>
                            </li>
                        ))}
                    </ul>
                </section>
            )}

            {cards.length > 0 && <h2 className={styles.heading}>Favoritter <span aria-hidden="true">♥</span></h2>}

            {favorites.length === 0 ? (
                <EmptyState icon="♡" title="Ingen favoritter ennå" action={{ href: "/", label: "Finn en bedrift" }}>
                    Trykk på «Lagre» på siden til en bedrift du liker. Da finner du den her, og kan bestille
                    samme tjeneste igjen med ett trykk.
                </EmptyState>
            ) : (
                <ul className={styles.grid}>
                    {favorites.map((favorite) => {
                        const last = favorite.lastVisit;
                        const again = last?.serviceAvailable
                            ? `/${favorite.slug}/bestill?${new URLSearchParams({
                                tjeneste: last.serviceId,
                                ...(last.employeeAvailable ? { ansatt: last.employeeId } : {}),
                            })}`
                            : null;
                        return (
                            <li key={favorite.businessId} className={`${panel.card} ${styles.item}`}>
                                <div className={styles.top}>
                                    <BusinessLogo name={favorite.name} category={favorite.category} logoUrl={favorite.logoUrl}/>
                                    <div>
                                        <h2 className={styles.name}>
                                            <Link href={`/${favorite.slug}`}>{favorite.name}</Link>
                                        </h2>
                                        <p className={styles.meta}>
                                            {getCategoryLabel(favorite.category)} · {favorite.city}
                                        </p>
                                        {/* Stars from 3 reviews, the same as everywhere else */}
                                        {favorite.averageRating !== null && favorite.reviewCount >= 3 && (
                                            <p className={styles.meta}>
                                                <Stars value={favorite.averageRating}/>{" "}
                                                {favorite.averageRating.toString().replace(".", ",")}
                                            </p>
                                        )}
                                    </div>
                                </div>
                                {favorite.nextVisitUtc ? (
                                    <p className={styles.next}>
                                        <span aria-hidden="true">📅</span> Neste time: {formatDate(favorite.nextVisitUtc)},{" "}
                                        {formatTime(favorite.nextVisitUtc)}
                                    </p>
                                ) : last && (
                                    <p className={panel.muted}>
                                        Sist: {last.serviceName} hos {last.employeeName}, {formatDate(last.startUtc)}
                                    </p>
                                )}
                                <div className={panel.actions}>
                                    {again ? (
                                        <Link href={again} className={`${panel.primaryButton} ${styles.linkButton}`}>
                                            Bestill igjen
                                        </Link>
                                    ) : (
                                        <Link href={`/${favorite.slug}`}
                                              className={`${panel.primaryButton} ${styles.linkButton}`}>
                                            Se tjenester
                                        </Link>
                                    )}
                                    <form action={toggleFavoriteAction}>
                                        <input type="hidden" name="businessId" value={favorite.businessId}/>
                                        <input type="hidden" name="on" value="0"/>
                                        <input type="hidden" name="tilbake" value="/min-besti/favoritter"/>
                                        <button type="submit" className={panel.secondaryButton}
                                                aria-label={`Fjern ${favorite.name} fra favoritter`}>
                                            Fjern
                                        </button>
                                    </form>
                                </div>
                            </li>
                        );
                    })}
                </ul>
            )}
        </main>
    );
}
