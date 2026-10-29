import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelStats } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { StatsNumbers } from "@/lib/types";
import { todayInOslo } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import BarChart from "./BarChart";
import { proInterestAction, weeklyReportAction } from "./actions";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Statistikk" };

const periods = [
    { days: 7, label: "7 dager" },
    { days: 30, label: "30 dager" },
    { days: 90, label: "90 dager" },
    { days: 365, label: "12 måneder" },
];

const weekdayShort = ["man", "tir", "ons", "tor", "fre", "lør", "søn"];

const kroner = new Intl.NumberFormat("nb-NO", { maximumFractionDigits: 0 });

function kr(amount: number) {
    return `${kroner.format(amount)} kr`;
}

// "+12 %" / "−5 %" / "som før" against the period before; nothing when there was nothing to compare with
function change(now: number, before: number) {
    if (before === 0) {
        return null;
    }
    const percent = Math.round(((now - before) / before) * 100);
    if (percent === 0) {
        return { text: "som før", className: "" };
    }
    return percent > 0
        ? { text: `+${percent} % mot forrige periode`, className: styles.up }
        : { text: `−${Math.abs(percent)} % mot forrige periode`, className: styles.down };
}

function Change({ now, before }: { now: number; before: number }) {
    const result = change(now, before);
    return result && <span className={`${styles.change} ${result.className}`}>{result.text}</span>;
}

function share(part: number, whole: number) {
    return whole === 0 ? 0 : Math.round((part / whole) * 100);
}

type StatsPageProps = {
    searchParams: Promise<{ dager?: string; lagret?: string; pro?: string; feil?: string }>;
};

// "Statistikk": what the business earned and how many came, what changed, and tips to get more bookings
export default async function StatsPage({ searchParams }: StatsPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const query = await searchParams;
    const days = periods.some((p) => String(p.days) === query.dager) ? Number(query.dager) : 30;
    const stats = await getPanelStats(token, days);
    if (!stats) {
        redirect("/logg-inn");
    }

    const now: StatsNumbers = stats.current;
    const before: StatsNumbers = stats.previous;
    const tips = stats.tips.filter((t) => t.kind === "tips");
    const pro = stats.tips.filter((t) => t.kind === "pro");
    // The hours that had visits, from the first to the last (not the whole night)
    const busyHours = stats.hours.map((count, hour) => ({ count, hour })).filter((h) => h.count > 0);
    const hours = busyHours.length === 0
        ? []
        : stats.hours.slice(busyHours[0].hour, busyHours[busyHours.length - 1].hour + 1)
            .map((count, i) => ({ count, hour: busyHours[0].hour + i }));
    const periodName = periods.find((p) => p.days === days)!.label;

    return (
        <main className={panel.main}>
            <PanelNav title="Statistikk"/>

            {query.lagret && <p className={styles.notice} role="status">Lagret.</p>}
            {query.feil && <p className={panel.error} role="alert">Det gikk ikke. Prøv igjen.</p>}

            <nav className={styles.periods} aria-label="Periode">
                {periods.map((p) => (
                    <Link key={p.days} href={`/panel/statistikk?dager=${p.days}`}
                          className={p.days === days ? `${styles.period} ${styles.periodActive}` : styles.period}
                          aria-current={p.days === days ? "page" : undefined}>
                        {p.label}
                    </Link>
                ))}
            </nav>

            <section className={styles.tiles} aria-label={`Siste ${periodName}`}>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Besøk</p>
                    <p className={styles.tileValue}>{now.visits}</p>
                    <Change now={now.visits} before={before.visits}/>
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Beregnet omsetning</p>
                    <p className={styles.tileValue}>{kr(now.revenue)}</p>
                    <Change now={now.revenue} before={before.revenue}/>
                    {now.visitsWithoutPrice > 0 && (
                        <span className={styles.tileNote}>
                            + {now.visitsWithoutPrice} besøk uten fast pris
                        </span>
                    )}
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Nye kunder</p>
                    <p className={styles.tileValue}>{now.newCustomers}</p>
                    <span className={styles.tileNote}>{now.returningCustomers} faste kunder kom tilbake</span>
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Neste 7 dager</p>
                    <p className={styles.tileValue}>{stats.upcomingVisits} {stats.upcomingVisits === 1 ? "time" : "timer"}</p>
                    {stats.upcomingRevenue > 0 && <span className={styles.tileNote}>{kr(stats.upcomingRevenue)}</span>}
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Møtte ikke / avbestilt</p>
                    <p className={styles.tileValue}>{now.noShows} / {now.cancelled}</p>
                    <span className={styles.tileNote}>
                        {share(now.noShows, now.visits + now.noShows)} % møtte ikke
                    </span>
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Bestilt på nett</p>
                    <p className={styles.tileValue}>{share(now.online, now.visits)} %</p>
                    <span className={styles.tileNote}>{now.online} på nett, {now.manual} lagt inn av deg</span>
                </div>
                <div className={styles.tile}>
                    <p className={styles.tileLabel}>Vurderinger</p>
                    <p className={styles.tileValue}>
                        {stats.averageStars === null ? "–" : `${stats.averageStars.toString().replace(".", ",")} ★`}
                    </p>
                    <span className={styles.tileNote}>
                        {stats.reviewCount} totalt{stats.newReviews > 0 ? `, ${stats.newReviews} nye` : ""}
                    </span>
                </div>
                {stats.occupancyPercent !== null && (
                    <div className={styles.tile}>
                        <p className={styles.tileLabel}>Belegg (omtrent)</p>
                        <p className={styles.tileValue}>{stats.occupancyPercent} %</p>
                        <span className={styles.tileNote}>av åpningstiden er booket</span>
                    </div>
                )}
            </section>

            {tips.length > 0 && (
                <section aria-labelledby="forslag">
                    <h2 id="forslag" className={styles.heading}>Forslag til deg</h2>
                    <ul className={styles.tips}>
                        {tips.map((tip) => (
                            <li key={tip.title} className={panel.card}>
                                <p className={styles.tipTitle}>{tip.title}</p>
                                <p className={styles.tipText}>{tip.text}</p>
                                {tip.link && <Link href={tip.link} className={styles.tipLink}>Gjør det nå →</Link>}
                            </li>
                        ))}
                    </ul>
                </section>
            )}

            <section aria-labelledby="utvikling">
                <h2 id="utvikling" className={styles.heading}>Utvikling</h2>
                <div className={styles.charts}>
                    <BarChart title="Besøk per uke, siste 12 uker" bars={stats.weeks.map((w) => {
                        const [, month, day] = w.weekStart.split("-");
                        return {
                            label: `${Number(day)}.${Number(month)}`,
                            value: w.visits,
                            detail: `Uka fra ${Number(day)}.${Number(month)}: ${w.visits} besøk, ${kr(w.revenue)}`,
                        };
                    })}/>
                    <BarChart title={`Besøk per ukedag, siste ${periodName}`} bars={stats.weekdays.map((count, i) => ({
                        label: weekdayShort[i],
                        value: count,
                        detail: `${weekdayShort[i]}: ${count} besøk`,
                    }))}/>
                    {hours.length > 0 && (
                        <BarChart title={`Travleste tider, siste ${periodName}`} bars={hours.map((h) => ({
                            label: String(h.hour),
                            value: h.count,
                            detail: `Kl. ${h.hour}–${h.hour + 1}: ${h.count} besøk`,
                        }))}/>
                    )}
                </div>
            </section>

            <div className={styles.tables}>
                {[{ title: "Populære tjenester", rows: stats.services }, { title: "Ansatte", rows: stats.employees }]
                    .filter((t) => t.rows.length > 0)
                    .map((t) => (
                        <section key={t.title} className={panel.card}>
                            <h2 className={styles.tableTitle}>{t.title}</h2>
                            <table className={styles.table}>
                                <thead>
                                <tr>
                                    <th scope="col">Navn</th>
                                    <th scope="col">Besøk</th>
                                    <th scope="col">Omsetning</th>
                                </tr>
                                </thead>
                                <tbody>
                                {t.rows.map((row) => (
                                    <tr key={row.name}>
                                        <th scope="row">{row.name}</th>
                                        <td>{row.visits}</td>
                                        <td>{kr(row.revenue)}</td>
                                    </tr>
                                ))}
                                </tbody>
                            </table>
                        </section>
                    ))}
            </div>

            {pro.length > 0 && (
                <section className={`${panel.card} ${styles.pro}`} aria-labelledby="pro">
                    <h2 id="pro" className={styles.heading}>Snart: BESTI Pro <span aria-hidden="true">✨</span></h2>
                    <ul className={styles.proList}>
                        {pro.map((tip) => (
                            <li key={tip.title}><strong>{tip.title}</strong> {tip.text}</li>
                        ))}
                    </ul>
                    <p className={styles.tipText}>
                        Pro gir aldri bedre plassering i søket. Rekkefølgen for kundene er alltid ærlig.
                    </p>
                    {stats.proInterest || query.pro ? (
                        <p className={styles.notice} role="status">Takk! Vi sier fra når Pro er klar.</p>
                    ) : (
                        <form action={proInterestAction}>
                            <input type="hidden" name="dager" value={days}/>
                            <SubmitButton className={panel.secondaryButton} pendingText="Sender …">
                                Jeg er interessert
                            </SubmitButton>
                        </form>
                    )}
                </section>
            )}

            <section className={`${panel.card} ${styles.report}`} aria-labelledby="ukerapport">
                <div>
                    <h2 id="ukerapport" className={styles.tableTitle}>Ukerapport på e-post</h2>
                    <p className={styles.tipText}>
                        {stats.weeklyReportOn
                            ? "Hver mandag morgen får du forrige ukes tall og et tips på e-post."
                            : "Slått av. Du får ingen ukerapport."}
                    </p>
                </div>
                <form action={weeklyReportAction}>
                    <input type="hidden" name="dager" value={days}/>
                    <input type="hidden" name="on" value={stats.weeklyReportOn ? "0" : "1"}/>
                    <SubmitButton className={panel.secondaryButton} pendingText="Lagrer …">
                        {stats.weeklyReportOn ? "Slå av" : "Slå på"}
                    </SubmitButton>
                </form>
            </section>

            <section className={`${panel.card} ${styles.report}`} aria-labelledby="eksport">
                <div>
                    <h2 id="eksport" className={styles.tableTitle}>Last ned bestillinger (Excel)</h2>
                    <p className={styles.tipText}>
                        Alle timer i perioden som en CSV-fil: dato, tjeneste, ansatt, kunde, status og pris.
                        Fint for regnskapsføreren.
                    </p>
                </div>
                <form method="get" action="/panel/eksport" className={styles.exportForm}>
                    <label>
                        Fra
                        <input type="date" name="fra" required defaultValue={`${todayInOslo().slice(0, 4)}-01-01`}/>
                    </label>
                    <label>
                        Til
                        <input type="date" name="til" required defaultValue={todayInOslo()}/>
                    </label>
                    <button type="submit" className={panel.secondaryButton}>Last ned</button>
                </form>
            </section>

            <p className={styles.footnote}>
                Omsetningen er beregnet ut fra prisen da timen ble bestilt. Kundene betaler hos dere, ikke i BESTI.
                Besøk = timer som er gjennomført (ikke avbestilte eller «Møtte ikke»).
            </p>
        </main>
    );
}
