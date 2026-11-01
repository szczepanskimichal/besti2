import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelLoyalty } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import ProBadge from "@/components/ProBadge/ProBadge";
import StampCard from "@/components/StampCard/StampCard";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import LoyaltyForm from "./LoyaltyForm";
import { proInterestAction, redeemLoyaltyAction } from "./actions";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Stempelkort" };

// Stamps come only from account bookings, so the customer card key is always "k-" + the user id
function customerHref(customerUserId: string) {
    return `/panel/kunder/${encodeURIComponent(`k-${customerUserId}`)}`;
}

type LoyaltyPageProps = {
    searchParams: Promise<{ lagret?: string; av?: string; gitt?: string; feil?: string; pro?: string }>;
};

// "Stempelkort": a digital loyalty card, in the dark glass look of the other panel pages. Stamps come by themselves from visits booked on BESTI
export default async function LoyaltyPage({ searchParams }: LoyaltyPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const card = await getPanelLoyalty(token);
    if (!card) {
        redirect("/logg-inn");
    }
    const query = await searchParams;
    const ready = card.customers.filter((c) => c.rewardReady);

    // Closest to a full card first
    const collecting = card.customers
        .filter((c) => !c.rewardReady && c.stamps > 0)
        .sort((a, b) => b.stamps - a.stamps);
    // The example card at the top: what a customer sees a few visits before the reward
    const exampleVisits = card.visits ?? 10;
    const exampleStamps = Math.max(1, exampleVisits - 3);

    return (
        <main className={panel.main}>
            <PanelNav title="Stempelkort"/>
            {query.lagret && <p className={styles.notice} role="status">Stempelkortet er lagret.</p>}
            {query.av && <p className={styles.notice} role="status">Stempelkortet er slått av.</p>}
            {query.gitt && <p className={styles.notice} role="status">Belønningen er registrert. Kunden starter på et nytt kort.</p>}
            {query.feil && <p className={styles.error} role="alert">{query.feil}</p>}

            {/* Top: the crystal card the customer sees, with the short rules under it */}
            <section className={styles.hero}>
                <p className={styles.heroLabel}>Slik ser kunden kortet</p>
                <div className={styles.exampleCard}>
                    <StampCard stamps={exampleStamps} visits={exampleVisits} crystal/>
                    <p className={styles.exampleReward}>
                        <CrystalIcon name="gave"/>
                        {/* A general example: each business decides for itself what the reward is */}
                        <span>Gratis BESTI-tjeneste</span>
                    </p>
                </div>
                <p className={styles.lead}>
                    Gi faste kunder en grunn til å komme tilbake. Hvert besøk som er bestilt på BESTI og gjennomført,
                    gir et stempel av seg selv. Kunden ser kortet på siden din og i Min BESTI.
                </p>
                <p className={styles.small}>
                    Timer du legger inn selv (telefon, drop-in) teller ikke, og heller ikke «Møtte ikke».
                    {card.startUtc && ` Kortet gjelder besøk fra ${formatDate(card.startUtc)}.`}
                </p>
                <div className={styles.pro}>
                    <ProBadge/>
                    <p className={styles.proText}>Blir en del av BESTI Pro når betaling starter.</p>
                    {query.pro ? (
                        <p className={styles.proThanks} role="status">Takk! Vi sier fra når Pro er klar.</p>
                    ) : (
                        <form action={proInterestAction}>
                            <SubmitButton className={styles.proButton} pendingText="Sender …">
                                Jeg vil være med i Pro
                            </SubmitButton>
                        </form>
                    )}
                </div>
            </section>

            <section className={styles.section}>
                <h2 className={styles.sectionTitle}>{card.visits ? "Innstillinger" : "Slå på stempelkort"}</h2>
                <LoyaltyForm visits={card.visits} reward={card.reward}/>
            </section>

            {card.visits && (
                <>
                    {/* Full cards: mint, because the customer has done their part */}
                    <section className={styles.section}>
                        <h2 className={styles.sectionTitle}>
                            Klar for belønning <span className={ready.length > 0 ? `${styles.count} ${styles.countLit}` : styles.count}>{ready.length}</span>
                        </h2>
                        {ready.length === 0 ? <p className={styles.small}>Ingen akkurat nå.</p> : (
                            <ul className={styles.list}>
                                {ready.map((customer) => (
                                    <li key={customer.customerUserId} className={styles.ready}>
                                        <span className={styles.giftRing}><CrystalIcon name="gave"/></span>
                                        <div className={styles.readyText}>
                                            {/* The name opens the customer card with all the details */}
                                            <Link href={customerHref(customer.customerUserId)} className={styles.name}>
                                                {customer.name}
                                            </Link>
                                            <p className={styles.rewardText}>{card.reward}</p>
                                        </div>
                                        <form action={redeemLoyaltyAction} className={styles.readyAction}>
                                            <input type="hidden" name="customerUserId" value={customer.customerUserId}/>
                                            <SubmitButton className={styles.saveButton} pendingText="Lagrer …">
                                                Belønning gitt
                                            </SubmitButton>
                                        </form>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </section>

                    <section className={styles.section}>
                        <h2 className={styles.sectionTitle}>
                            Samler stempler <span className={collecting.length > 0 ? `${styles.count} ${styles.countLit}` : styles.count}>{collecting.length}</span>
                        </h2>
                        {collecting.length === 0 ? (
                            <p className={styles.small}>Ingen ennå. Stempler kommer etter neste besøk.</p>
                        ) : (
                            <ul className={styles.list}>
                                {collecting.map((customer) => (
                                    <li key={customer.customerUserId} className={styles.collecting}>
                                        <div className={styles.collectingHead}>
                                            {/* The whole row opens the customer card (stretched link) */}
                                            <Link href={customerHref(customer.customerUserId)}
                                                  className={`${styles.name} ${styles.rowLink}`}>
                                                {customer.name}
                                            </Link>
                                            <p className={styles.stampCount}>
                                                <strong>{customer.stamps}</strong> av {card.visits}
                                            </p>
                                        </div>
                                        <StampCard stamps={customer.stamps} visits={card.visits!} crystal/>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </section>
                </>
            )}
        </main>
    );
}
