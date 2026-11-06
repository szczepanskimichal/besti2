import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness, getPanelQr } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { SITE_URL } from "@/lib/site";
import PrintButton from "./PrintButton";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Plakat",
};

// An A5 poster for the counter or the window: the name, a big QR code and the short address.
// "Nordlys" = BESTI's look (night sky with aurora); "Lys" = white paper and dark text, which saves ink
export default async function PosterPage({ searchParams }: { searchParams: Promise<{ stil?: string }> }) {
    const light = (await searchParams).stil === "lys";
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const [business, qr] = await Promise.all([getPanelBusiness(token), getPanelQr(token, "svg")]);
    if (!business || !qr) {
        redirect("/logg-inn");
    }
    const shortLink = `${SITE_URL}/${business.slug}`.replace(/^https?:\/\//, "");

    return (
        // print-page: globals.css hides the header, footer and background when printing
        <main className={`${styles.main} print-page`}>
            {/* Plakat (Ada 2026-11-01): the dark glass look of the panel. The tools in a glass card, the poster under it */}
            <div className={styles.toolbar}>
                <div className={styles.toolbarTop}>
                    <h1 className={styles.title}>Plakat</h1>
                    <Link href="/panel/del" className={styles.back}>← Tilbake</Link>
                </div>
                <div className={styles.styles} role="group" aria-label="Utseende">
                    <Link href="/panel/del/plakat" aria-current={light ? undefined : "page"}>Nordlys</Link>
                    <Link href="/panel/del/plakat?stil=lys" aria-current={light ? "page" : undefined}>Lys (sparer blekk)</Link>
                </div>
                <PrintButton className={styles.printButton}/>
                <p className={styles.hint}>Velg papir A5 (eller A4 for en større plakat) og «Ingen marger» hvis du kan.</p>
            </div>

            <article className={light ? styles.sheet : `${styles.sheet} ${styles.aurora}`}>
                {/* The name in Syne at the top, in both looks (michal 2026-11-04) */}
                <p className={styles.brand}>
                    {/* The new logo B with the star on the left, only on the aurora poster (on white paper it looks blurry) */}
                    {!light && (
                        // eslint-disable-next-line @next/next/no-img-element -- small print logo, next/image not needed
                        <img src="/logo/besti-f1.svg" alt="" className={styles.brandIcon}/>
                    )}
                    BESTI
                </p>
                {business.logoUrl && (
                    // eslint-disable-next-line @next/next/no-img-element -- our own resized WebP from the API
                    <img src={business.logoUrl} alt="" className={styles.logo}/>
                )}
                <p className={styles.name}>{business.name}</p>
                <h2 className={styles.heading}>Bestill time hos oss</h2>
                {/* The crystal arrow from Del og få kunder points at the code */}
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                <img src="/ikoner/pil-ned.svg" alt="" aria-hidden="true" className={styles.arrow}/>
                {/* The QR code stays black on white on both posters: phones read that best */}
                {/* eslint-disable-next-line @next/next/no-img-element -- a small data: image from our own API */}
                <img src={qr} alt={`QR-kode til ${shortLink}`} className={styles.qr}/>
                <p className={styles.scan}>Skann koden med kameraet på mobilen</p>
                <p className={styles.link}>{shortLink}</p>
                {/* The BESTI slogan at the bottom, with the short promise under it (michal 2026-11-04) */}
                <div className={styles.footer}>
                    <p className={styles.slogan}>Bestill med din BESTI</p>
                    <p className={styles.promise}>Raskt, enkelt og hyggelig, døgnet rundt.</p>
                </div>
            </article>
        </main>
    );
}
