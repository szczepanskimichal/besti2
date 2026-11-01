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
    { icon: "krystall", title: "Statistikk for 90 dager og 12 måneder", text: "Se hvordan bedriften vokser over tid." },
];

// BESTI Pro (Ada 2026-11-01, v5): what Pro is, that it is free in the beta, what it will cost and what is always free.
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
                <section className={styles.info} aria-labelledby="pro-info">
                    <h2 id="pro-info" className={styles.infoTitle}>
                        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2"
                             strokeLinecap="round" aria-hidden="true">
                            <circle cx="12" cy="12" r="9.5"/>
                            <path d="M12 11v6"/>
                            <circle cx="12" cy="7.5" r="0.6" fill="currentColor"/>
                        </svg>
                        Gratis nå, betalt senere
                    </h2>
                    <ol className={styles.steps}>
                        <li><span className={styles.stepNumber} aria-hidden="true">1</span>
                            <p><b>Nå, i beta:</b> alt i Pro er gratis for alle bedrifter.</p></li>
                        <li><span className={styles.stepNumber} aria-hidden="true">2</span>
                            <p><b>Når betaling med kort og Vipps kommer:</b> Pro blir en betalt tjeneste, 299 kr i måneden.</p></li>
                        <li><span className={styles.stepNumber} aria-hidden="true">3</span>
                            <p><b>Før det:</b> du får e-post minst 30 dager før. Ingenting koster noe uten at du har sagt ja.</p></li>
                    </ol>
                </section>

                <section className={`${styles.card} ${styles.price}`} aria-label="Pris">
                    <p className={styles.big}>
                        <span className={styles.priceBadge}><ProBadge short size={46}/></span>
                        <span className={styles.number}>299</span>
                        <span className={styles.perMonth}>kr / mnd</span>
                    </p>
                    <p className={styles.per}>per salong, eks. mva</p>
                    <span className={styles.betaPill}>Gratis i beta</span>
                </section>

                <section className={styles.card} aria-labelledby="pro-med">
                    <h2 id="pro-med" className={styles.cardTitle}>Med i Pro</h2>
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
                    </ul>
                </section>

                <section className={`${styles.card} ${styles.lilac}`} aria-labelledby="pro-kommer">
                    <h2 id="pro-kommer" className={`${styles.cardTitle} ${styles.lilacTitle}`}>Kommer i Pro</h2>
                    <ul className={styles.list}>
                        <li>
                            <span className={styles.lock} aria-hidden="true"><LockIcon/></span>
                            <span><b>SMS til faste kunder</b>Send en melding til alle kundene med ett trykk. Låst i beta, fordi SMS koster penger.</span>
                        </li>
                    </ul>
                </section>

                <section className={`${styles.card} ${styles.center}`} aria-labelledby="pro-gratis">
                    <h2 id="pro-gratis" className={styles.cardTitle}>Alltid gratis</h2>
                    <p className={styles.text}>
                        Kalender, nettbestilling, kunder, ansatte, varsler, e-post til faste kunder, BESTI sammen og ukerapporten.
                    </p>
                    <p className={styles.text}>Pro gir aldri bedre plassering i søket. Rekkefølgen for kundene er alltid ærlig.</p>
                </section>

                <section id="med" className={`${styles.card} ${styles.center}`}>
                    {stats.proInterest || query.pro ? (
                        <p className={styles.thanks} role="status">Takk! Vi sier fra når Pro starter.</p>
                    ) : (
                        <>
                            <p className={styles.text}>Vil du være med når Pro starter?</p>
                            <form action={proPageInterestAction}>
                                <SubmitButton className={styles.button} pendingText="Sender …">Jeg vil være med</SubmitButton>
                            </form>
                        </>
                    )}
                    {query.feil && <p className={styles.small} role="alert">Noe gikk galt. Prøv igjen.</p>}
                </section>
            </div>
        </main>
    );
}
