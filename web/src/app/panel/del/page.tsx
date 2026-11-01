import type { Metadata } from "next";
import type { ReactNode } from "react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness, getPanelQr, getPanelStats } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { SITE_URL } from "@/lib/site";
import CopyButton from "@/components/CopyButton/CopyButton";
import LockIcon from "@/components/LockIcon/LockIcon";
import ProBadge from "@/components/ProBadge/ProBadge";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import { smsProInterestAction } from "./actions";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Del og få kunder",
};

// Steps as glowing round numbers, the same counters as on the other panel pages
function Steps({ items }: { items: ReactNode[] }) {
    return (
        <ol className={styles.steps}>
            {items.map((item, index) => (
                <li key={index} className={styles.step}>
                    <span className={styles.stepNumber} aria-hidden="true">{index + 1}</span>
                    <span className={styles.stepText}>{item}</span>
                </li>
            ))}
        </ol>
    );
}

// "Del og få kunder" (Ada 2026-11-01): the dark glass look of the other panel pages. The link in a glowing mint
// field on top, the QR code in a white box with a mint neon frame, ready texts in dark fields with a Kopier pill
type SharePageProps = {
    searchParams: Promise<{ pro?: string; feil?: string }>;
};

export default async function SharePage({ searchParams }: SharePageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const [business, qrSvg, qrPng, stats, query] = await Promise.all([
        getPanelBusiness(token), getPanelQr(token, "svg"), getPanelQr(token, "png"), getPanelStats(token, 7), searchParams,
    ]);
    if (!business || !qrSvg || !qrPng) {
        redirect("/logg-inn");
    }

    const link = `${SITE_URL}/${business.slug}`;
    const shortLink = link.replace(/^https?:\/\//, "");
    // ?utm_source=...: the visit statistics show where the customers came from
    const linkFor = (source: string) => `${link}?utm_source=${source}`;
    const message = `Hei! Nå kan du bestille time hos ${business.name} på nett, når det passer deg: ${linkFor("epost")}`;
    const bio = `Bestill time på nett 👇\n${shortLink}`;

    return (
        <main className={`${panel.main} ${styles.page}`}>
            <PanelNav title="Del og få kunder"/>
            <p className={styles.lead}>
                De fleste nye kunder kommer via deg: fra plakaten i salongen, Instagram og Google. Her er alt du trenger.
            </p>
            {business.isHidden && (
                <p className={styles.waiting} role="status">
                    Bedriften venter på godkjenning. Lenken virker for kundene så snart den er godkjent.
                </p>
            )}

            <div className={styles.grid}>
                <section className={`${styles.card} ${styles.hero}`}>
                    <h2 className={styles.cardTitle}>Lenken til siden din</h2>
                    <a href={link} className={styles.link} target="_blank" rel="noopener noreferrer">{shortLink}</a>
                    <div className={styles.actions}>
                        <CopyButton text={link} label="Kopier lenken" className={styles.saveButton}/>
                        <a href={link} className={`${styles.pill} ${styles.pillLit}`} target="_blank" rel="noopener noreferrer">Se siden</a>
                    </div>
                </section>

                <section className={`${styles.card} ${styles.qrCard}`}>
                    <h2 className={styles.cardTitle}>QR-kode og plakat</h2>
                    <div className={styles.qrRow}>
                        {/* eslint-disable-next-line @next/next/no-img-element -- a small data: image from our own API */}
                        <img src={qrSvg} alt={`QR-kode til ${shortLink}`} className={styles.qr}/>
                        <div className={styles.qrText}>
                            <p className={styles.text}>Kunden skanner koden med kameraet på mobilen og kommer rett til siden din.</p>
                            <Link href="/panel/del/plakat" className={styles.saveButton}>Skriv ut plakat (A5)</Link>
                            <div className={styles.actions}>
                                <a href={qrPng} download="besti-qr.png" className={`${styles.pill} ${styles.pillLit}`}>Last ned PNG</a>
                                <a href={qrSvg} download="besti-qr.svg" className={`${styles.pill} ${styles.pillLit}`}>Last ned SVG</a>
                            </div>
                        </div>
                    </div>
                    <p className={styles.tip}>
                        <span className={styles.bulb} aria-hidden="true">
                            {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                            <img src="/ikoner/lyspare.svg" alt=""/>
                        </span>
                        <span>Tips: heng plakaten ved kassen eller i vinduet, og legg QR-koden på visittkort og timekort.
                        SVG er skarp i alle størrelser (til trykkeri), PNG passer til Instagram og Facebook.</span>
                    </p>
                </section>

                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Google: «Bestill time» rett i søket</h2>
                    <p className={styles.text}>Når noen søker på bedriften din, kan Google vise en knapp som går rett til BESTI.</p>
                    <Steps items={[
                        <>Søk på navnet til bedriften på Google, logget inn med kontoen som eier Google-profilen
                            (eller åpne <a href="https://business.google.com" target="_blank" rel="noopener noreferrer">business.google.com</a>).</>,
                        <>Trykk «Rediger profil» og velg «Kontakt».</>,
                        <>Lim inn lenken under «Lenker for timebestilling» (kan også hete «Avtalelenker») og lagre.</>,
                    ]}/>
                    <CopyButton text={linkFor("google")} label="Kopier lenke til Google" className={`${styles.pill} ${styles.pillLit}`}/>
                    <p className={styles.small}>Google sjekker endringen først, så det kan ta noen dager før knappen vises.</p>
                </section>

                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Instagram og Facebook</h2>
                    <Steps items={[
                        <>Instagram: «Rediger profil» → «Lenker» → «Legg til ekstern lenke», og lim inn lenken.</>,
                        <>Facebook-siden: «Legg til knapp» → «Bestill nå», og lim inn lenken.</>,
                        <>Skriv gjerne i bio-teksten at kundene kan bestille på nett.</>,
                    ]}/>
                    {/* On the page a crystal arrow; the copied text keeps the emoji, which Instagram can show */}
                    <p className={styles.ready}>
                        Bestill time på nett
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                        <img src="/ikoner/pil-ned.svg" alt="" aria-hidden="true" className={styles.arrow}/>
                        <br/><span className={styles.bioLink}>{shortLink}</span>
                    </p>
                    <div className={styles.actions}>
                        <CopyButton text={bio} label="Kopier bio-tekst" className={`${styles.pill} ${styles.pillLit}`}/>
                        <CopyButton text={linkFor("instagram")} label="Lenke til Instagram" className={`${styles.pill} ${styles.pillLit}`}/>
                        <CopyButton text={linkFor("facebook")} label="Lenke til Facebook" className={`${styles.pill} ${styles.pillLit}`}/>
                    </div>
                </section>

                <section id="melding" className={`${styles.card} ${styles.wide}`}>
                    <h2 className={styles.cardTitle}>Melding til faste kunder</h2>
                    <span className={styles.free}>
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4"
                             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                            <path d="M3 7l9 6 9-6"/>
                            <rect x="3" y="5" width="18" height="14" rx="3"/>
                        </svg>
                        E-post er gratis
                    </span>
                    <p className={styles.text}>Send den på e-post til kundene dine, eller legg den i en story.</p>
                    <p className={styles.ready}>{message}</p>
                    <CopyButton text={message} label="Kopier meldingen" className={`${styles.pill} ${styles.pillLit}`}/>
                    {/* SMS from BESTI costs money per message: shown, but locked until Pro, with a way to say "I want it" */}
                    <div className={styles.proBox}>
                        <p className={styles.proTitle}>
                            <ProBadge short/> SMS kun i BESTI Pro <LockIcon className={styles.lock} keyhole/>
                        </p>
                        <p className={styles.text}>
                            Send meldingen som SMS til alle kundene dine med ett trykk, rett fra BESTI.
                        </p>
                        <button type="button" className={`${styles.pill} ${styles.locked}`} disabled
                                aria-label="Send som SMS (låst, kommer i BESTI Pro)">
                            <LockIcon className={styles.lock}/> Send som SMS
                        </button>
                        <p className={styles.small}>
                            SMS koster penger per melding, derfor er den låst i beta. Vil du ha det? Trykk under, så sier vi fra når det starter.
                        </p>
                        {stats?.proInterest || query.pro ? (
                            <p className={styles.thanks} role="status">Takk! Vi sier fra når det er klart.</p>
                        ) : (
                            <form action={smsProInterestAction}>
                                <SubmitButton className={`${styles.pill} ${styles.pillLit}`} pendingText="Sender …">
                                    Jeg vil være med
                                </SubmitButton>
                            </form>
                        )}
                        {query.feil && <p className={styles.small} role="alert">Noe gikk galt. Prøv igjen.</p>}
                    </div>
                </section>
            </div>
        </main>
    );
}
