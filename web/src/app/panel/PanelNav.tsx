import type { ReactNode } from "react";
import Link from "next/link";
import ProBadge from "@/components/ProBadge/ProBadge";
import styles from "./page.module.css";

// One thin line icon per page, so the long row is easy to scan with a thumb
const ICONS: Record<string, string> = {
    kalender: "M4 6h16v14H4zM4 10h16M8 3v4M16 3v4",
    liste: "M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01",
    kunder: "M16 19v-1a4 4 0 0 0-8 0v1M12 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6M20 19v-1a3 3 0 0 0-2-2.8M17 5.2a3 3 0 0 1 0 5.6",
    statistikk: "M4 20V10M10 20V4M16 20v-7M22 20H2",
    tjenester: "M6 3l4 9-4 9M14 3l4 9-4 9",
    ansatte: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8M4 21a8 8 0 0 1 16 0",
    profil: "M3 21V9l9-6 9 6v12zM9 21v-6h6v6",
    galleri: "M3 5h18v14H3zM3 16l5-5 4 4 3-3 6 6M15 9h.01",
    vurderinger: "M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z",
    stempelkort: "M3 7h18v10H3zM7 12h.01M12 12h.01M17 12h.01",
    apningstider: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18M12 7v5l3 2",
    pro: "M3 8l4.5 4L12 5l4.5 7L21 8l-2 11H5z",
    del: "M18 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6M6 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6M18 22a3 3 0 1 0 0-6 3 3 0 0 0 0 6M8.6 13.5l6.8 4M15.4 6.5l-6.8 4",
};

const PAGES: { href: string; label: string; icon: string; pro?: boolean }[] = [
    { href: "/panel", label: "Kalender", icon: "kalender" },
    { href: "/panel/liste", label: "Liste", icon: "liste" },
    { href: "/panel/kunder", label: "Kunder", icon: "kunder" },
    { href: "/panel/statistikk", label: "Statistikk", icon: "statistikk" },
    { href: "/panel/tjenester", label: "Tjenester", icon: "tjenester" },
    { href: "/panel/ansatte", label: "Ansatte", icon: "ansatte" },
    { href: "/panel/profil", label: "Profil", icon: "profil" },
    { href: "/panel/galleri", label: "Galleri", icon: "galleri" },
    { href: "/panel/vurderinger", label: "Vurderinger", icon: "vurderinger" },
    { href: "/panel/stempelkort", label: "Stempelkort", icon: "stempelkort", pro: true },
    { href: "/panel/apningstider", label: "Åpningstider", icon: "apningstider" },
    { href: "/panel/del", label: "Del og få kunder", icon: "del" },
    // Last, across the whole row: what Pro is, what it will cost and what is always free
    { href: "/panel/pro", label: "BESTI Pro", icon: "pro", pro: true },
];

// "Lørdag 31.10.2026": the owner sees which day it is today, on every panel page
function todayLabel(): string {
    const now = new Date();
    const weekday = new Intl.DateTimeFormat("nb-NO", { timeZone: "Europe/Oslo", weekday: "long" }).format(now);
    const date = new Intl.DateTimeFormat("nb-NO", { timeZone: "Europe/Oslo", day: "2-digit", month: "2-digit", year: "numeric" }).format(now);
    return `${weekday.charAt(0).toUpperCase()}${weekday.slice(1)} ${date}`;
}

// The top bar of the panel: title and one row of all panel pages, the open one lit in mint
export default function PanelNav({ title, active }: { title: ReactNode; active?: string }) {
    const current = active ?? title;
    return (
        <div className={styles.topBar}>
            <div className={styles.titleRow}>
                <h1 className={styles.title}>{title}</h1>
                <p className={styles.today}>
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.8"
                         strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                        <path d="M4 6h16v14H4zM4 10h16M8 3v4M16 3v4"/>
                    </svg>
                    <span className={styles.srOnlyToday}>I dag er det </span>{todayLabel()}
                </p>
            </div>
            <nav className={styles.topActions} aria-label="Panel">
                {PAGES.map((p) => (
                    <Link key={p.href} href={p.href} aria-current={p.label === current ? "page" : undefined}
                          className={p.icon === "pro" ? `${styles.navTab} ${styles.navProTab}` : styles.navTab}>
                        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="1.7"
                             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                            <path d={ICONS[p.icon]}/>
                        </svg>
                        <span>{p.label}</span>
                        {p.pro && p.icon !== "pro" && <span className={styles.navPro}><ProBadge short/></span>}
                    </Link>
                ))}
            </nav>
        </div>
    );
}
