import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness, getPanelQr } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { SITE_URL } from "@/lib/site";
import CopyButton from "@/components/CopyButton/CopyButton";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Del og få kunder",
};

// Everything a business needs to bring its own customers: the link, the QR code, the poster and ready texts
export default async function SharePage() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const [business, qrSvg, qrPng] = await Promise.all([
        getPanelBusiness(token), getPanelQr(token, "svg"), getPanelQr(token, "png"),
    ]);
    if (!business || !qrSvg || !qrPng) {
        redirect("/logg-inn");
    }

    const link = `${SITE_URL}/${business.slug}`;
    const shortLink = link.replace(/^https?:\/\//, "");
    // ?utm_source=...: the visit statistics show where the customers came from
    const linkFor = (source: string) => `${link}?utm_source=${source}`;
    const message = `Hei! Nå kan du bestille time hos ${business.name} på nett, når det passer deg: ${linkFor("sms")}`;
    const bio = `Bestill time på nett 👇\n${shortLink}`;

    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Del og få kunder</h1>
            <p className={styles.muted}>
                De fleste nye kunder kommer via deg: fra plakaten i salongen, Instagram og Google. Her er alt du trenger.
            </p>
            {business.isHidden && (
                <p className={styles.muted}>
                    Bedriften venter på godkjenning. Lenken virker for kundene så snart den er godkjent.
                </p>
            )}

            <section className={styles.card}>
                <h2>Lenken til siden din</h2>
                <a href={link} className={styles.link} target="_blank" rel="noopener noreferrer">{shortLink}</a>
                <div className={styles.actions}>
                    <CopyButton text={link} label="Kopier lenken" className={styles.primaryButton}/>
                    <a href={link} className={styles.secondaryButton} target="_blank" rel="noopener noreferrer">Se siden</a>
                </div>
            </section>

            <section className={styles.card}>
                <h2>QR-kode og plakat</h2>
                <div className={styles.qrRow}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- a small data: image from our own API */}
                    <img src={qrSvg} alt={`QR-kode til ${shortLink}`} className={styles.qr}/>
                    <div>
                        <p>Kunden skanner koden med kameraet på mobilen og kommer rett til siden din.</p>
                        <div className={styles.actions}>
                            <Link href="/panel/del/plakat" className={styles.primaryButton}>Skriv ut plakat (A5)</Link>
                            <a href={qrPng} download="besti-qr.png" className={styles.secondaryButton}>Last ned PNG</a>
                            <a href={qrSvg} download="besti-qr.svg" className={styles.secondaryButton}>Last ned SVG</a>
                        </div>
                    </div>
                </div>
                <p className={styles.small}>
                    Tips: heng plakaten ved kassen eller i vinduet, og legg QR-koden på visittkort og timekort.
                    SVG er skarp i alle størrelser (til trykkeri), PNG passer til Instagram og Facebook.
                </p>
            </section>

            <section className={styles.card}>
                <h2>Google: «Bestill time» rett i søket</h2>
                <p>Når noen søker på bedriften din, kan Google vise en knapp som går rett til BESTI.</p>
                <ol>
                    <li>Søk på navnet til bedriften på Google, logget inn med kontoen som eier Google-profilen
                        (eller åpne <a href="https://business.google.com" target="_blank" rel="noopener noreferrer">business.google.com</a>).
                    </li>
                    <li>Trykk «Rediger profil» og velg «Kontakt».</li>
                    <li>Lim inn lenken under «Lenker for timebestilling» (kan også hete «Avtalelenker») og lagre.</li>
                </ol>
                <div className={styles.actions}>
                    <CopyButton text={linkFor("google")} label="Kopier lenke til Google" className={styles.secondaryButton}/>
                </div>
                <p className={styles.small}>Google sjekker endringen først, så det kan ta noen dager før knappen vises.</p>
            </section>

            <section className={styles.card}>
                <h2>Instagram og Facebook</h2>
                <ol>
                    <li>Instagram: «Rediger profil» → «Lenker» → «Legg til ekstern lenke», og lim inn lenken.</li>
                    <li>Facebook-siden: «Legg til knapp» → «Bestill nå», og lim inn lenken.</li>
                    <li>Skriv gjerne i bio-teksten at kundene kan bestille på nett.</li>
                </ol>
                <p className={styles.link}>{bio}</p>
                <div className={styles.actions}>
                    <CopyButton text={bio} label="Kopier bio-tekst" className={styles.secondaryButton}/>
                    <CopyButton text={linkFor("instagram")} label="Kopier lenke til Instagram" className={styles.secondaryButton}/>
                    <CopyButton text={linkFor("facebook")} label="Kopier lenke til Facebook" className={styles.secondaryButton}/>
                </div>
            </section>

            <section className={styles.card}>
                <h2>Melding til faste kunder</h2>
                <p>Send den på SMS eller e-post til kundene dine, eller legg den i en story.</p>
                <p className={styles.link}>{message}</p>
                <div className={styles.actions}>
                    <CopyButton text={message} label="Kopier meldingen" className={styles.secondaryButton}/>
                </div>
            </section>
        </main>
    );
}
