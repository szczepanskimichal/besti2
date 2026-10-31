import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getMe, getMyFavorites, getMyLoyalty } from "@/lib/api";
import type { LoyaltyCard } from "@/lib/types";
import { getToken } from "@/lib/auth";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate, formatTime } from "@/lib/format";
import { getInitials } from "@/components/BusinessLogo/BusinessLogo";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import StampCard from "@/components/StampCard/StampCard";
import Stars from "@/components/Stars/Stars";
import { toggleFavoriteAction } from "./actions";
import panel from "../../panel/page.module.css";
import styles from "./page.module.css";
import EmptyState from "@/components/EmptyState/EmptyState";

export const metadata: Metadata = { title: "Favoritter" };

// "Stempelkort" inside the card of its business: crystal hearts for the visits, a crystal gift as the reward
function StampBox({ card }: { card: LoyaltyCard }) {
    return (
        <div className={card.rewardReady ? `${styles.stamps} ${styles.ready}` : styles.stamps}>
            <p className={styles.label}>Stempelkort</p>
            <StampCard stamps={card.stamps} visits={card.visits} crystal/>
            <p className={styles.stampText}>
                {card.rewardReady
                    ? <>Fullt kort! Si fra ved neste besøk, så får du <strong>{card.reward}</strong>.</>
                    : <>{card.visits - card.stamps} besøk igjen til <strong>{card.reward}</strong>.</>}
            </p>
        </div>
    );
}

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
    const others = cards.filter((c) => !favorites.some((f) => f.businessId === c.businessId));

    return (
        <main className={panel.main}>
            <h1 className={panel.title}>Min BESTI</h1>
            <MinBestiTabs/>
            {favoritt === "feil" && <p className={panel.error} role="alert">Det gikk ikke. Prøv igjen.</p>}

            {favorites.length === 0 ? (
                <EmptyState icon="♡" title="Ingen favoritter ennå" action={{ href: "/", label: "Finn en bedrift" }}>
                    Trykk på «Lagre» på siden til en bedrift du liker. Da finner du den her, og kan bestille
                    samme tjeneste igjen med ett trykk.
                </EmptyState>
            ) : (
                <>
                <h2 className={styles.title}>Mine BESTI-favoritter <CrystalIcon name="hjerte"/></h2>
                <ul className={styles.grid}>
                    {favorites.map((favorite) => {
                        const last = favorite.lastVisit;
                        const again = last?.serviceAvailable
                            ? `/${favorite.slug}/bestill?${new URLSearchParams({
                                tjeneste: last.serviceId,
                                ...(last.employeeAvailable ? { ansatt: last.employeeId } : {}),
                            })}`
                            : null;
                        const card = cards.find((c) => c.businessId === favorite.businessId);
                        return (
                            <li key={favorite.businessId} className={`${panel.card} ${styles.item}`}>
                                {/* eslint-disable-next-line @next/next/no-img-element -- decorative SVG */}
                                <img src="/ikoner/hjerte.svg" alt="" className={styles.heart}/>
                                <div className={styles.top}>
                                    {favorite.logoUrl ? (
                                        // eslint-disable-next-line @next/next/no-img-element -- small logo from our own API
                                        <img src={favorite.logoUrl} alt="" className={styles.logo}/>
                                    ) : (
                                        <span className={styles.logo} aria-hidden="true">{getInitials(favorite.name)}</span>
                                    )}
                                    <div>
                                        <h2 className={styles.name}>
                                            <Link href={`/${favorite.slug}`}>{favorite.name}</Link>
                                        </h2>
                                        <p className={styles.meta}>
                                            {getCategoryLabel(favorite.category)} · {favorite.city}
                                        </p>
                                        {/* Stars from 3 reviews, the same as everywhere else */}
                                        {favorite.averageRating !== null && favorite.reviewCount >= 3 && (
                                            <p className={styles.rating}>
                                                <Stars value={favorite.averageRating}/>{" "}
                                                {favorite.averageRating.toString().replace(".", ",")}
                                            </p>
                                        )}
                                    </div>
                                </div>
                                {favorite.nextVisitUtc ? (
                                    <p className={styles.next}>
                                        <span className={styles.label}>Neste time</span>
                                        <span>{formatDate(favorite.nextVisitUtc)}</span>
                                        <span>kl. {formatTime(favorite.nextVisitUtc)}</span>
                                    </p>
                                ) : last && (
                                    <p className={styles.last}>
                                        <span className={styles.label}>Sist</span>
                                        <span>{last.serviceName} hos {last.employeeName}, {formatDate(last.startUtc)}</span>
                                    </p>
                                )}
                                {/* The stamp card of this business, only when it has one */}
                                {card && <StampBox card={card}/>}
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
                </>
            )}

            {/* Stamp cards from businesses that are not (or no longer) favourites */}
            {others.length > 0 && (
                <section aria-labelledby="stempelkort" className={styles.section}>
                    <h2 id="stempelkort" className={styles.heading}>Stempelkort</h2>
                    <ul className={styles.grid}>
                        {others.map((card) => (
                            <li key={card.businessId} className={panel.card}>
                                <h3 className={styles.name}><Link href={`/${card.businessSlug}`}>{card.businessName}</Link></h3>
                                <StampBox card={card}/>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
        </main>
    );
}
