import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelCustomers, getPanelLoyalty } from "@/lib/api";
import { getToken } from "@/lib/auth";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import styles from "./page.module.css";
import kunder from "./kunder.module.css";
import NotePad, { BookIcon } from "./NotePad";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import EmptyState from "@/components/EmptyState/EmptyState";

export const metadata: Metadata = { title: "Kunder" };

type CustomersPageProps = {
    searchParams: Promise<{ q?: string; lagret?: string }>;
};

// Everyone who has booked here, made from the bookings themselves, and the customers imported from the old
// system: visits, last and next time, and the business's own note. Customers who deleted their account
// (or are older than 3 years) are not here
export default async function CustomersPage({ searchParams }: CustomersPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const { q: rawQ, lagret } = await searchParams;
    const q = (rawQ ?? "").trim().slice(0, 100);
    const [customers, loyalty] = await Promise.all([getPanelCustomers(token, q), getPanelLoyalty(token).catch(() => null)]);
    if (!customers) {
        redirect("/logg-inn");
    }
    // Stempelkort: the stamps of customers with an account (customer key "k-" + their user id)
    const cardSize = loyalty?.visits ?? null;
    const stampsByKey = new Map((loyalty?.customers ?? []).map((c) => [`k-${c.customerUserId}`, c]));

    return (
        <main className={panel.main}>
            <PanelNav title="Kunder"/>
            <div className={kunder.toolbar}>
                <form action="/panel/kunder" className={kunder.search} role="search">
                    <label className={styles.srOnly} htmlFor="kunde-sok">Søk etter kunde</label>
                    <svg className={kunder.searchIcon} width="18" height="18" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
                        <circle cx="11" cy="11" r="7"/>
                        <path d="M20 20l-3.5-3.5"/>
                    </svg>
                    <input id="kunde-sok" name="q" type="search" defaultValue={q} maxLength={100}
                           placeholder="Navn, telefon eller e-post" className={kunder.searchInput}/>
                    <button type="submit" className={kunder.searchButton}>Søk</button>
                </form>
                <Link href="/panel/kunder/importer" className={kunder.importButton}>
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"
                         strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                        <path d="M12 4v11M7 10l5 5 5-5M5 20h14"/>
                    </svg>
                    Importer kunder
                </Link>
            </div>
            <div className={kunder.summary}>
                <p className={kunder.count}>
                    <strong>{customers.length}</strong> {customers.length === 1 ? "kunde" : "kunder"}
                </p>
                <p className={kunder.privacy}>
                    <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.5"
                         aria-hidden="true">
                        <rect x="3" y="7" width="10" height="7" rx="1.5"/>
                        <path d="M5.5 7V5a2.5 2.5 0 0 1 5 0v2"/>
                    </svg>
                    Notatene ser bare dere og de ansatte, ikke kunden i BESTI.
                </p>
            </div>
            {customers.length === 0 ? (
                q ? <p className={panel.muted}>Ingen treff.</p> : (
                    <EmptyState icon="👥" title="Ingen kunder ennå">
                        Kundene kommer hit av seg selv med den første bestillingen, med historikk og plass til notater.
                        Eller importer dem fra systemet dere bruker i dag.
                    </EmptyState>
                )
            ) : (
                <ul className={kunder.cards}>
                    {customers.map((customer) => (
                        <li key={customer.key}>
                            <div className={kunder.card}>
                                <span className={kunder.avatar} aria-hidden="true">{initials(customer.name)}</span>
                                <span className={kunder.who}>
                                    <Link href={`/panel/kunder/${encodeURIComponent(customer.key)}`} className={kunder.name}>
                                        {customer.name}
                                    </Link>
                                    <span className={kunder.contact}>
                                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                             strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                                            <path d="M5 3h4l2 5-2.5 1.5a11 11 0 0 0 6 6L16 13l5 2v4a2 2 0 0 1-2 2A17 17 0 0 1 3 5a2 2 0 0 1 2-2z"/>
                                        </svg>
                                        {customer.phone}
                                    </span>
                                    {customer.email && (
                                        <span className={kunder.contact}>
                                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                                 strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                                                <rect x="3" y="5" width="18" height="14" rx="2"/>
                                                <path d="M3 7l9 6 9-6"/>
                                            </svg>
                                            <span className={kunder.email}>{customer.email}</span>
                                        </span>
                                    )}
                                    {customer.note !== undefined ? (
                                        <NotePad customerKey={customer.key} name={customer.name} note={customer.note ?? ""}
                                                 open={lagret === customer.key}/>
                                    ) : (
                                        // The API does not send the note text yet: open the customer card instead,
                                        // so an empty notepad never overwrites a saved note
                                        <Link href={`/panel/kunder/${encodeURIComponent(customer.key)}`}
                                              className={`${kunder.contact} ${kunder.noteSummary} ${kunder.noteLink} ${customer.hasNote ? "" : kunder.noteEmpty}`}>
                                            <BookIcon/>
                                            <span>{customer.hasNote ? "Notat" : "Skriv notat"}</span>
                                        </Link>
                                    )}
                                </span>
                                <span className={kunder.side}>
                                    <span className={kunder.visits}>
                                        <strong>{customer.visits}</strong>
                                        <small>besøk</small>
                                    </span>
                                    {cardSize !== null && stampsByKey.has(customer.key) && (
                                        <Stamps stamps={stampsByKey.get(customer.key)!.stamps} size={cardSize}
                                                ready={stampsByKey.get(customer.key)!.rewardReady}/>
                                    )}
                                </span>
                                <span className={kunder.facts}>
                                    {customer.nextVisitUtc && (
                                        <span className={kunder.next}>Neste {shortDate(customer.nextVisitUtc)}</span>
                                    )}
                                    {customer.lastVisitUtc && <span className={kunder.fact}>Sist {shortDate(customer.lastVisitUtc)}</span>}
                                    {customer.noShows > 0 && (
                                        <span className={kunder.noShow}>Møtte ikke {customer.noShows}×</span>
                                    )}
                                    {customer.imported
                                        ? <span className={kunder.quiet}>Importert</span>
                                        : !customer.hasAccount && <span className={kunder.quiet}>Lagt inn av dere</span>}
                                </span>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}

// "Stempelkort" under the visits: crystal heart and how far the customer is, or the gift when the card is full
function Stamps({ stamps, size, ready }: { stamps: number; size: number; ready: boolean }) {
    return (
        <span className={ready ? `${kunder.stamps} ${kunder.stampsReady}` : kunder.stamps}
              title={ready ? "Fullt stempelkort, belønningen kan gis" : `${stamps} av ${size} stempler`}>
            <span className={kunder.stampsCount}>
                <CrystalIcon name={ready ? "gave" : "hjerte"}/>
                {ready ? "klar" : `${stamps}/${size}`}
            </span>
            <small>{ready ? "belønning" : "stempler"}</small>
        </span>
    );
}

// "Nora Lie" -> "NL", "Kari" -> "K"
function initials(name: string) {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part.charAt(0).toUpperCase()).join("");
}

const osloShortDate = new Intl.DateTimeFormat("nb-NO", { timeZone: "Europe/Oslo", day: "numeric", month: "short" });

// "14. nov." without the weekday, short enough for a pill
function shortDate(utc: string) {
    return osloShortDate.format(new Date(utc)).replace(/\.$/, "");
}
