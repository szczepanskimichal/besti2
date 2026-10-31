import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getPanelCustomer } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatTime } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { removeCustomerAction } from "../actions";
import CustomerNoteForm from "./CustomerNoteForm";
import panel from "../../page.module.css";
import styles from "../page.module.css";

export const metadata: Metadata = { title: "Kundekort" };

const kroner = new Intl.NumberFormat("nb-NO", { maximumFractionDigits: 0 });

type CustomerPageProps = {
    params: Promise<{ key: string }>;
    searchParams: Promise<{ lagret?: string }>;
};

// One customer: contact, every visit here, what they spent, and the business's own note
export default async function CustomerPage({ params, searchParams }: CustomerPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const { key } = await params;
    const card = await getPanelCustomer(token, decodeURIComponent(key));
    if (!card) {
        notFound();
    }
    const { lagret } = await searchParams;
    const customer = card.customer;

    return (
        <main className={panel.main}>
            <Link href="/panel/kunder" className={styles.back}>← Alle kunder</Link>
            <h1 className={panel.title}>{customer.name}</h1>
            <p className={panel.customer}>
                <a href={`tel:${customer.phone}`}>{customer.phone}</a>
                {customer.email && <> · <a href={`mailto:${customer.email}`}>{customer.email}</a></>}
            </p>
            <p className={styles.facts}>
                <span>{customer.visits} besøk</span>
                <span>ca. {kroner.format(card.spentNok)} kr totalt</span>
                {customer.noShows > 0 && <span className={styles.noShow}>Møtte ikke: {customer.noShows}</span>}
                <span>
                    {customer.hasAccount ? "Har BESTI-konto" : customer.imported ? "Importert fra et annet system" : "Lagt inn av dere"}
                </span>
            </p>

            <section className={panel.card} aria-labelledby="notat">
                <h2 id="notat" className={styles.cardTitle}>Notat (bare for dere)</h2>
                {lagret && <p className={styles.saved} role="status">Notatet er lagret.</p>}
                <CustomerNoteForm customerKey={customer.key} note={card.note ?? ""}/>
                {card.noteUpdatedUtc && <p className={panel.muted}>Sist endret {formatDate(card.noteUpdatedUtc)}</p>}
            </section>

            {customer.imported ? (
                <section className={panel.card} aria-labelledby="fjern">
                    <h2 id="fjern" className={styles.cardTitle}>Ikke bestilt hos dere i BESTI ennå</h2>
                    <p className={styles.noteHint}>
                        Kunden kom fra kundelisten dere importerte. Er kunden ikke deres lenger, eller vil kunden
                        ikke være med, kan dere fjerne kunden og notatet.
                    </p>
                    <form action={removeCustomerAction} className={styles.importActions}>
                        <input type="hidden" name="key" value={customer.key}/>
                        <SubmitButton className={panel.dangerButton} pendingText="Fjerner…">Fjern fra Kunder</SubmitButton>
                    </form>
                </section>
            ) : <h2 className={panel.sectionTitle}>Timer</h2>}
            <ul className={panel.list}>
                {card.visits.map((visit) => (
                    <li key={visit.id} className={panel.card}>
                        <div className={panel.cardHeader}>
                            <p className={panel.when}>{formatDate(visit.startUtc)}, {formatTime(visit.startUtc)}</p>
                            <span className={`${panel.status} ${panel[visit.status] ?? ""}`}>
                                {statusLabels[visit.status] ?? visit.status}
                            </span>
                        </div>
                        <p className={panel.service}>
                            {visit.serviceName} hos {visit.employeeName}
                            {visit.priceNok !== null && ` · ${kroner.format(visit.priceNok)} kr`}
                        </p>
                        {visit.customerNote && <p className={panel.note}>💬 {visit.customerNote}</p>}
                    </li>
                ))}
            </ul>
        </main>
    );
}
