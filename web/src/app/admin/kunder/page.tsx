import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { findAdminCustomers, getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { statusLabels } from "@/lib/bookingStatus";
import { formatDate, formatDateTime } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { resendAction } from "../actions";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "Kunder" };

type AdminCustomersPageProps = {
    searchParams: Promise<{ q?: string; sendt?: string; feil?: string }>;
};

// Support: "jeg fikk aldri e-posten", "jeg kommer ikke inn". Only the exact e-mail or phone number finds someone:
// there is deliberately no list of all customers
export default async function AdminCustomersPage({ searchParams }: AdminCustomersPageProps) {
    const query = await searchParams;
    const q = (query.q ?? "").trim().slice(0, 250);
    const token = await getToken();
    const me = token ? await getMe(token) : null;
    const customers = token && me?.isAdmin ? (q ? await findAdminCustomers(token, q) : []) : null;
    if (!customers) {
        notFound();
    }

    return (
        <>
            <h1 className={styles.title}>Kunder</h1>
            <p className={styles.muted}>
                Skriv hele e-postadressen eller telefonnummeret til kunden som trenger hjelp.
                Det finnes ingen liste over alle kunder (personvern).
            </p>
            <form action="/admin/kunder" className={styles.search} role="search">
                <label className={styles.srOnly} htmlFor="kunde-sok">E-post eller telefon</label>
                <input id="kunde-sok" name="q" type="search" defaultValue={q} maxLength={250}
                       placeholder="kari@gmail.com eller 912 34 567" className={styles.searchInput}/>
                <button type="submit" className={styles.secondaryButton}>Finn</button>
            </form>
            {query.sendt && <p className={styles.notice} role="status">Ny bekreftelseslenke er sendt.</p>}
            {query.feil && <p className={styles.error} role="alert">{query.feil}</p>}
            {q && customers.length === 0 && (
                <p className={styles.muted}>Fant ingen kundekonto med akkurat dette. Sjekk skrivemåten, eller spør om bedriftskonto.</p>
            )}
            <ul className={styles.list}>
                {customers.map((customer) => (
                    <li key={customer.id} className={styles.card}>
                        <div className={styles.cardHeader}>
                            <span className={styles.name}>{customer.fullName ?? "Uten navn"}</span>
                            <span className={`${styles.status} ${customer.emailConfirmed ? styles.Synlig : styles.Venter}`}>
                                {customer.emailConfirmed ? "E-post bekreftet" : "E-post ikke bekreftet"}
                            </span>
                        </div>
                        <dl className={styles.details}>
                            <dt>E-post</dt>
                            <dd><a href={`mailto:${customer.email}`}>{customer.email}</a></dd>
                            <dt>Telefon</dt>
                            <dd>{customer.phone ?? "–"}</dd>
                            <dt>Innlogging</dt>
                            <dd>{[customer.hasPassword && "Passord", customer.hasGoogle && "Google"].filter(Boolean).join(" og ") || "Ingen ennå"}</dd>
                            <dt>Konto laget</dt>
                            <dd>{customer.createdUtc ? formatDate(customer.createdUtc) : "Ukjent"}</dd>
                            <dt>Besøk</dt>
                            <dd>{customer.visits}, {customer.reviews} vurderinger</dd>
                            {customer.staffAt.length > 0 && (
                                <>
                                    <dt>Ansatt hos</dt>
                                    <dd>{customer.staffAt.join(", ")}</dd>
                                </>
                            )}
                        </dl>
                        {!customer.emailConfirmed && (
                            <form action={resendAction} className={styles.reasonForm}>
                                <input type="hidden" name="id" value={customer.id}/>
                                <input type="hidden" name="q" value={q}/>
                                <SubmitButton className={styles.primaryButton} pendingText="Sender …">Send ny bekreftelseslenke</SubmitButton>
                            </form>
                        )}
                        <h2 className={`${styles.cardTitle} ${styles.sectionGap}`}>Kommende timer</h2>
                        {customer.upcoming.length === 0 ? <p className={styles.muted}>Ingen.</p> : (
                            <ul className={styles.setup}>
                                {customer.upcoming.map((booking) => (
                                    <li key={booking.id}>
                                        {formatDateTime(booking.startUtc)}: {booking.serviceName} hos {booking.businessName}
                                        {" "}({statusLabels[booking.status] ?? booking.status})
                                    </li>
                                ))}
                            </ul>
                        )}
                        <h2 className={`${styles.cardTitle} ${styles.sectionGap}`}>E-post som venter</h2>
                        {customer.waitingMails.length === 0 ? <p className={styles.muted}>Ingen – alt er sendt.</p> : (
                            <ul className={styles.setup}>
                                {customer.waitingMails.map((mail) => (
                                    <li key={`${mail.createdUtc}-${mail.subject}`}>
                                        «{mail.subject}» fra {formatDateTime(mail.createdUtc)}
                                        {mail.attempts > 0 && <span className={styles.bad}> · {mail.attempts} feil, prøver igjen {formatDateTime(mail.nextAttemptUtc)}</span>}
                                    </li>
                                ))}
                            </ul>
                        )}
                    </li>
                ))}
            </ul>
        </>
    );
}
