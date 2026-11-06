import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPanelStats } from "@/lib/api";
import { getToken } from "@/lib/auth";
import LockIcon from "@/components/LockIcon/LockIcon";
import ProBadge from "@/components/ProBadge/ProBadge";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import { proPageInterestAction } from "./actions";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "BESTI Pro",
};

// What is in Pro, with the crystal icons from the rest of the panel
const IN_PRO = [
    { icon: "kalender", title: "Venteliste og «Ledig i dag»", text: "Blir en time ledig, får kundene på ventelisten beskjed med en gang." },
    { icon: "bjelle", title: "«Tid for ny time?»", text: "Kundene får en påminnelse når det er tid for neste besøk." },
    { icon: "stjerne", title: "Stempelkort", text: "Faste kunder samler stempler og kommer tilbake." },
    { icon: "krystall", title: "Statistikk for 30 dager, 90 dager og 12 måneder", text: "Se hvordan bedriften vokser over tid." },
];

// Coming to Pro later, shown locked with a lilac "Kommer" (they were "Kommer snart i Pro" on Statistikk before)
const COMING = [
    { title: "SMS til faste kunder", text: "Låst i beta, fordi SMS koster penger." },
    { title: "Hvor kommer kundene fra?", text: "Se om kundene kom fra Instagram, Google eller QR-plakaten." },
    { title: "Tilbud for rolige timer", text: "Fyll de rolige dagene med tilbud og fremhev populære tjenester." },
    { title: "Flere avdelinger", text: "Alle avdelinger og ansatte samlet i ett panel." },
];

// Everything a business gets without Pro
const INCLUDED = [
    "Kalender", "Nettbestilling", "Kunder", "Ansatte", "Varsler",
    "E-post til faste kunder", "BESTI Duo og Team", "Ukerapporten",
    "Egen bedriftsside", "Galleri", "Ekte vurderinger", "QR-plakat",
    "Statistikk for 7 dager", "Eksport av data",
];

// BESTI Pro (Ada 2026-11-03, v6): three cards. What is in Pro (free in the beta), no price yet, what is included without Pro.
// Mint = free now, lilac = Pro and what comes later. Pro never gives a better place in the search
type ProPageProps = {
    searchParams: Promise<{ pro?: string; feil?: string }>;
};

export default async function ProPage({ searchParams }: ProPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const [stats, query] = await Promise.all([getPanelStats(token, 7), searchParams]);
    if (!stats) {
        redirect("/logg-inn");
    }

    return (
        <main className={`${panel.main} ${styles.page}`}>
            <PanelNav title={<>BESTI Pro <ProBadge short size={38}/></>} active="BESTI Pro"/>
            <p className={styles.lead}>Verktøy som fyller tomme timer og får faste kunder til å komme tilbake.</p>

            <div className={styles.grid}>
                {/* Ada 2026-11-03: one card says what Pro is and that it is free now (was steps + two lists) */}
                <section className={styles.info} aria-labelledby="pro-info">
                    <h2 id="pro-info" className={`${styles.infoTitle} ${styles.lilacTitle}`}>
                        Prøv alt i Pro gratis i beta
                    </h2>
                    <ul className={styles.list}>
                        {IN_PRO.map((item) => (
                            <li key={item.title}>
                                <span className={styles.icon} aria-hidden="true">
                                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                                    <img src={`/ikoner/${item.icon}.svg`} alt=""/>
                                </span>
                                <span><b>{item.title}</b>{item.text}</span>
                            </li>
                        ))}
                        {COMING.map((item) => (
                            <li key={item.title}>
                                <span className={styles.lock} aria-hidden="true"><LockIcon/></span>
                                <span><b>{item.title} <span className={styles.soon}>Kommer</span></b>{item.text}</span>
                            </li>
                        ))}
                    </ul>
                    {/* Ada 2026-11-04: SMS is not part of the Pro price, say so before anyone asks (no price yet) */}
                    <p className={`${styles.small} ${styles.smsNote}`}>
                        SMS betales per melding, i tillegg til Pro. Du bestemmer selv et tak per måned. Prisen får du vite før start.
                    </p>
                </section>

                {/* No price yet (michal 2026-11-06): free in the beta, the price comes later, the 30-day promise and the button */}
                <section id="med" className={`${styles.card} ${styles.price}`} aria-labelledby="pro-pris">
                    <h2 id="pro-pris" className={`${styles.priceTitle} ${styles.lilacTitle}`}>Pris</h2>
                    <p className={styles.big}>
                        <span className={styles.priceBadge}><ProBadge short size={60}/></span>
                        <span className={`${styles.number} ${styles.numberText}`}>Gratis i beta</span>
                    </p>
                    <p className={styles.per}>Prisen etter beta får du vite senere</p>
                    <p className={styles.text}>Du får e-post minst 30 dager før. Ingenting koster noe uten at du har sagt ja.</p>
                    {stats.proInterest || query.pro ? (
                        <p className={styles.thanks} role="status">Takk! Vi sier fra når Pro starter.</p>
                    ) : (
                        <form action={proPageInterestAction}>
                            <SubmitButton className={styles.button} pendingText="Sender …">Jeg vil være med</SubmitButton>
                        </form>
                    )}
                    {query.feil && <p className={styles.small} role="alert">Noe gikk galt. Prøv igjen.</p>}
                </section>

                {/* Ada 2026-11-03: a big list, so the business sees how much is included without Pro */}
                <section className={`${styles.card} ${styles.center} ${styles.wide}`} aria-labelledby="pro-gratis">
                    <h2 id="pro-gratis" className={styles.freeTitle}>Inkludert i BESTI</h2>
                    <ul className={styles.freeList}>
                        {INCLUDED.map((item) => <li key={item}>{item}</li>)}
                    </ul>
                    <p className={styles.text}>Pro gir deg flere verktøy, ikke bedre plass i søket. Alle bedrifter vises på like vilkår.</p>
                </section>
            </div>
        </main>
    );
}
