import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";
import { redirect } from "next/navigation";
import { getMe, getMyYear } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatLength } from "@/lib/format";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import BarChart from "../../panel/statistikk/BarChart";
import panel from "../../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Mitt BESTI-år" };

const monthNames = ["januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober",
    "november", "desember"];

const kroner = new Intl.NumberFormat("nb-NO", { maximumFractionDigits: 0 });

type MyYearPageProps = {
    searchParams: Promise<{ ar?: string }>;
};

// "Mitt BESTI-år": the customer's own year in numbers, a little celebration of taking time for yourself
export default async function MyYearPage({ searchParams }: MyYearPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/mitt-ar");
    }
    const { ar } = await searchParams;
    const chosen = ar && /^20\d{2}$/.test(ar) ? Number(ar) : undefined;
    const [me, year] = await Promise.all([getMe(token), getMyYear(token, chosen)]);
    if (me?.isBusiness) {
        redirect("/panel");
    }
    if (!year) {
        redirect("/logg-inn?neste=/min-besti/mitt-ar");
    }
    const thisYear = new Date().getFullYear();
    const soFar = year.year === thisYear;
    const firstName = me?.fullName?.split(" ")[0];
    const facts = [
        year.minutesOfMeTime > 0 && { icon: "⏳", value: formatLength(year.minutesOfMeTime), text: "tid til deg selv" },
        { icon: <CrystalIcon name="pinne"/>, value: String(year.businesses), text: year.businesses === 1 ? "sted" : "forskjellige steder" },
        year.favoriteBusiness && {
            icon: "💛",
            value: year.favoriteBusiness,
            text: `din favoritt, ${year.favoriteBusinessVisits} ${year.favoriteBusinessVisits === 1 ? "gang" : "ganger"}`,
            href: year.favoriteBusinessSlug ? `/${year.favoriteBusinessSlug}` : undefined,
        },
        year.favoriteService && { icon: "✂️", value: year.favoriteService, text: "det du bestilte mest" },
        year.busiestMonth && { icon: "📅", value: monthNames[year.busiestMonth - 1], text: "din travleste måned" },
        year.visitsWithFriends > 0 && {
            icon: "👯",
            value: String(year.visitsWithFriends),
            text: year.visitsWithFriends === 1 ? "gang sammen med venner" : "ganger sammen med venner",
        },
        year.reviewsWritten > 0 && {
            icon: <CrystalIcon name="stjerne"/>,
            value: String(year.reviewsWritten),
            text: year.reviewsWritten === 1 ? "vurdering som hjelper andre" : "vurderinger som hjelper andre",
        },
        year.spent > 0 && { icon: "💳", value: `ca. ${kroner.format(year.spent)} kr`, text: "brukt på deg selv" },
    ].filter((f) => !!f) as { icon: ReactNode; value: string; text: string; href?: string }[];

    return (
        <main className={panel.main}>
            <h1 className={panel.title}>Min Besti</h1>
            <MinBestiTabs/>

            {year.years.length > 1 && (
                <nav className={styles.years} aria-label="År">
                    {year.years.map((y) => (
                        <Link key={y} href={`/min-besti/mitt-ar?ar=${y}`}
                              className={y === year.year ? `${styles.yearLink} ${styles.yearActive}` : styles.yearLink}
                              aria-current={y === year.year ? "page" : undefined}>
                            {y}
                        </Link>
                    ))}
                </nav>
            )}

            <section className={styles.hero}>
                <p className={styles.kicker}>{firstName ? `${firstName}s` : "Ditt"} {year.year} med BESTI</p>
                {year.visits === 0 ? (
                    <>
                        <p className={styles.big}>Ingen besøk {soFar ? "ennå" : `i ${year.year}`}</p>
                        <p className={styles.sub}>
                            {soFar ? "Når du har vært på ditt første besøk, fyller vi året ditt her." : "Velg et annet år over."}
                        </p>
                        <Link href="/" className={`${panel.primaryButton} ${styles.cta}`}>Finn en ledig time</Link>
                    </>
                ) : (
                    <>
                        <p className={styles.big}>{year.visits}</p>
                        <p className={styles.sub}>
                            besøk {soFar ? "så langt i år" : `i ${year.year}`}
                            {year.firstVisitUtc && ` · det første ${formatDate(year.firstVisitUtc)}`}
                        </p>
                    </>
                )}
            </section>

            {year.visits > 0 && (
                <>
                    <ul className={styles.facts}>
                        {facts.map((fact) => (
                            <li key={fact.text} className={styles.fact}>
                                <span className={styles.icon} aria-hidden="true">{fact.icon}</span>
                                <span className={styles.value}>
                                    {fact.href ? <Link href={fact.href}>{fact.value}</Link> : fact.value}
                                </span>
                                <span className={styles.text}>{fact.text}</span>
                            </li>
                        ))}
                    </ul>
                    <BarChart title={`Besøk per måned i ${year.year}`} bars={year.months.map((count, i) => ({
                        label: monthNames[i].slice(0, 3),
                        value: count,
                        detail: `${monthNames[i]}: ${count} besøk`,
                    }))}/>
                    <p className={styles.note}>
                        Bare besøk som er gjennomført og bestilt med kontoen din teller. Beløpet er beregnet ut fra
                        prisene på BESTI.
                    </p>
                </>
            )}
        </main>
    );
}
