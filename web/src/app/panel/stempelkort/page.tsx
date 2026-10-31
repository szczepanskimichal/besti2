import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPanelLoyalty } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import ProBadge from "@/components/ProBadge/ProBadge";
import StampCard from "@/components/StampCard/StampCard";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import LoyaltyForm from "./LoyaltyForm";
import { redeemLoyaltyAction, saveLoyaltyAction } from "./actions";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Stempelkort" };

type LoyaltyPageProps = {
    searchParams: Promise<{ lagret?: string; av?: string; gitt?: string; feil?: string }>;
};

// "Stempelkort": a digital loyalty card. Stamps come by themselves from visits booked on BESTI
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
    const collecting = card.customers.filter((c) => !c.rewardReady && c.stamps > 0);

    return (
        <main className={panel.main}>
            <PanelNav title="Stempelkort"/>
            {query.lagret && <p className={styles.notice} role="status">Stempelkortet er lagret.</p>}
            {query.av && <p className={styles.notice} role="status">Stempelkortet er slått av.</p>}
            {query.gitt && <p className={styles.notice} role="status">Belønningen er registrert. Kunden starter på et nytt kort.</p>}
            {query.feil && <p className={panel.error} role="alert">{query.feil}</p>}

            <section className={panel.card}>
                <p className={panel.muted}>
                    <ProBadge/> Gratis i beta. Når betaling starter, blir stempelkort en del av BESTI Pro.
                </p>
                <p className={styles.lead}>
                    Gi faste kunder en grunn til å komme tilbake. Hvert besøk som er bestilt på BESTI og gjennomført,
                    gir et stempel av seg selv. Kunden ser kortet på siden din og i Min BESTI.
                </p>
                <p className={panel.muted}>
                    Timer du legger inn selv (telefon, drop-in) teller ikke, og heller ikke «Møtte ikke».
                    {card.startUtc && ` Kortet gjelder besøk fra ${formatDate(card.startUtc)}.`}
                </p>
            </section>

            <h2 className={styles.heading}>{card.visits ? "Innstillinger" : "Slå på stempelkort"}</h2>
            <LoyaltyForm visits={card.visits} reward={card.reward}/>
            {card.visits && (
                <form action={saveLoyaltyAction} className={styles.off}>
                    <input type="hidden" name="off" value="1"/>
                    <SubmitButton className={panel.dangerButton} pendingText="Slår av …">Slå av stempelkort</SubmitButton>
                    <span className={panel.muted}>Slår du det på igjen senere, starter alle på null.</span>
                </form>
            )}

            {card.visits && (
                <>
                    <h2 className={styles.heading}>Klar for belønning 🎁</h2>
                    {ready.length === 0 ? <p className={panel.muted}>Ingen akkurat nå.</p> : (
                        <ul className={panel.list}>
                            {ready.map((customer) => (
                                <li key={customer.customerUserId} className={`${panel.card} ${styles.ready}`}>
                                    <div className={panel.cardHeader}>
                                        <p className={panel.when}>{customer.name}</p>
                                        <span className={panel.muted}>Sist {formatDate(customer.lastVisitUtc)}</span>
                                    </div>
                                    <p>Har fullt kort: <strong>{card.reward}</strong></p>
                                    <form action={redeemLoyaltyAction}>
                                        <input type="hidden" name="customerUserId" value={customer.customerUserId}/>
                                        <SubmitButton className={panel.primaryButton} pendingText="Lagrer …">
                                            Belønning gitt
                                        </SubmitButton>
                                    </form>
                                </li>
                            ))}
                        </ul>
                    )}

                    <h2 className={styles.heading}>Samler stempler</h2>
                    {collecting.length === 0 ? <p className={panel.muted}>Ingen ennå. Stempler kommer etter neste besøk.</p> : (
                        <ul className={panel.list}>
                            {collecting.map((customer) => (
                                <li key={customer.customerUserId} className={panel.card}>
                                    <div className={panel.cardHeader}>
                                        <p className={panel.when}>{customer.name}</p>
                                        <span className={panel.muted}>{customer.stamps} av {card.visits}</span>
                                    </div>
                                    <StampCard stamps={customer.stamps} visits={card.visits!} small/>
                                </li>
                            ))}
                        </ul>
                    )}
                </>
            )}
        </main>
    );
}
