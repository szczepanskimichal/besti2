import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelCustomers } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Kunder" };

type CustomersPageProps = {
    searchParams: Promise<{ q?: string }>;
};

// Everyone who has booked here, made from the bookings themselves: visits, last and next time, and the
// business's own note. Customers who deleted their account (or are older than 3 years) are not here
export default async function CustomersPage({ searchParams }: CustomersPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const q = ((await searchParams).q ?? "").trim().slice(0, 100);
    const customers = await getPanelCustomers(token, q);
    if (!customers) {
        redirect("/logg-inn");
    }

    return (
        <main className={panel.main}>
            <PanelNav title="Kunder"/>
            <form action="/panel/kunder" className={styles.search} role="search">
                <label className={styles.srOnly} htmlFor="kunde-sok">Søk etter kunde</label>
                <input id="kunde-sok" name="q" type="search" defaultValue={q} maxLength={100}
                       placeholder="Navn, telefon eller e-post" className={styles.searchInput}/>
                <button type="submit" className={panel.secondaryButton}>Søk</button>
            </form>
            <p className={panel.muted}>
                {customers.length} {customers.length === 1 ? "kunde" : "kunder"}. Notatene ser bare dere – aldri kunden.
            </p>
            {customers.length === 0 ? (
                <p className={panel.muted}>{q ? "Ingen treff." : "Ingen kunder ennå. De kommer med den første bestillingen."}</p>
            ) : (
                <ul className={panel.list}>
                    {customers.map((customer) => (
                        <li key={customer.key} className={panel.card}>
                            <div className={panel.cardHeader}>
                                <Link href={`/panel/kunder/${encodeURIComponent(customer.key)}`} className={styles.name}>
                                    {customer.name}
                                </Link>
                                {customer.hasNote && <span className={styles.noteBadge}>📝 Notat</span>}
                            </div>
                            <p className={styles.facts}>
                                <span>{customer.visits} besøk</span>
                                {customer.lastVisitUtc && <span>Sist {formatDate(customer.lastVisitUtc)}</span>}
                                {customer.nextVisitUtc && <span className={styles.next}>Neste {formatDate(customer.nextVisitUtc)}</span>}
                                {customer.noShows > 0 && <span className={styles.noShow}>Møtte ikke: {customer.noShows}</span>}
                                {!customer.hasAccount && <span>Lagt inn av dere</span>}
                            </p>
                            <p className={panel.customer}>
                                <a href={`tel:${customer.phone}`}>{customer.phone}</a>
                            </p>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
