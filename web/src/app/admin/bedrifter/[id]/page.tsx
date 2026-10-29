import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { decideAction, noteAction } from "../../actions";
import NoteForm from "./NoteForm";
import styles from "../../admin.module.css";

export const metadata: Metadata = { title: "Bedrift" };

type AdminBusinessPageProps = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ avvis?: string; lagret?: string; godkjent?: string; feil?: string }>;
};

// One business: contact details for a call, what is missing in "Kom i gang", and BESTI's own note
export default async function AdminBusinessPage({ params, searchParams }: AdminBusinessPageProps) {
    const { id } = await params;
    const query = await searchParams;
    const token = await getToken();
    const details = token && /^[0-9a-f-]{36}$/i.test(id) ? await getAdminBusiness(token, id) : null;
    if (!details) {
        notFound();
    }
    const business = details.business;
    const waiting = business.status === "Venter";

    return (
        <>
            <Link href="/admin/bedrifter" className={styles.back}>← Alle bedrifter</Link>
            <h1 className={styles.title}>{business.name}</h1>
            <p className={styles.muted}>
                {getCategoryLabel(business.category)} · {business.city} · {business.status === "Synlig"
                    ? <Link href={`/${business.slug}`}>bestiapp.no/{business.slug}</Link>
                    : business.status}
            </p>
            {query.lagret && <p className={styles.notice} role="status">Notatet er lagret.</p>}
            {query.godkjent && <p className={styles.notice} role="status">Godkjent! Eieren har fått e-post.</p>}
            {query.feil && <p className={styles.error} role="alert">{query.feil}</p>}

            {waiting && (
                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Venter på godkjenning</h2>
                    {query.avvis ? (
                        <>
                            <p>
                                Avvis betyr at registreringen og kontoen slettes, og eieren får e-post om det.
                                Det kan ikke angres. Bruk det bare når personen ikke representerer bedriften.
                            </p>
                            <div className={styles.actions}>
                                <form action={decideAction}>
                                    <input type="hidden" name="id" value={business.id}/>
                                    <input type="hidden" name="decision" value="avvis"/>
                                    <SubmitButton className={styles.dangerButton} pendingText="Avviser …">Ja, avvis og slett</SubmitButton>
                                </form>
                                <Link href={`/admin/bedrifter/${business.id}`} className={styles.secondaryButton}>Avbryt</Link>
                            </div>
                        </>
                    ) : (
                        <div className={styles.actions}>
                            <form action={decideAction}>
                                <input type="hidden" name="id" value={business.id}/>
                                <input type="hidden" name="decision" value="godkjenn"/>
                                <SubmitButton className={styles.primaryButton} pendingText="Godkjenner …">Godkjenn</SubmitButton>
                            </form>
                            <Link href={`/admin/bedrifter/${business.id}?avvis=1`} className={styles.dangerButton}>Avvis …</Link>
                        </div>
                    )}
                </section>
            )}

            <div className={styles.columns}>
                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Kontakt</h2>
                    <dl className={styles.details}>
                        <dt>Innlogging</dt>
                        <dd>{business.ownerEmail ? <a href={`mailto:${business.ownerEmail}`}>{business.ownerEmail}</a> : "–"}</dd>
                        <dt>Telefon</dt>
                        <dd><a href={`tel:${business.phoneNumber}`}>{business.phoneNumber}</a></dd>
                        <dt>E-post til kunder</dt>
                        <dd>{details.publicEmail ?? "–"}</dd>
                        <dt>Nettside</dt>
                        <dd>{details.website ? <a href={details.website} rel="noopener noreferrer" target="_blank">{details.website}</a> : "–"}</dd>
                        <dt>Adresse</dt>
                        <dd>{details.address}, {details.postalCode} {business.city}</dd>
                        <dt>Org.nr.</dt>
                        <dd>
                            <a href={`https://virksomhet.brreg.no/nb/oppslag/enheter/${details.orgNumber}`}
                               rel="noopener noreferrer" target="_blank">{details.orgNumber}</a>
                            {business.verified ? " · Verifisert" : ""}
                        </dd>
                        <dt>Hørte om BESTI</dt>
                        <dd>{details.referralSource ?? "Ikke oppgitt"}</dd>
                    </dl>
                </section>

                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Status</h2>
                    <dl className={styles.details}>
                        <dt>Registrert</dt>
                        <dd>{formatDate(business.createdUtc)}</dd>
                        <dt>Godkjent</dt>
                        <dd>{details.approvedUtc ? formatDate(details.approvedUtc) : "–"}</dd>
                        <dt>Siste bestilling laget</dt>
                        <dd>{details.lastBookingCreatedUtc ? formatDate(details.lastBookingCreatedUtc) : "Ingen ennå"}</dd>
                        <dt>Bestillinger (30 d)</dt>
                        <dd>{business.bookingsLast30Days}, {business.upcoming} kommende</dd>
                        <dt>Vurderinger</dt>
                        <dd>{details.reviewCount}{business.averageStars !== null ? `, snitt ${business.averageStars.toString().replace(".", ",")} ★` : ""}</dd>
                        <dt>Ukerapport</dt>
                        <dd>{details.weeklyReportOn ? "På" : "Slått av"}</dd>
                        <dt>BESTI Pro</dt>
                        <dd>{business.proInterest ? "Interessert" : "–"}</dd>
                    </dl>
                    <h3 className={styles.cardTitle}>Kom i gang ({business.setupDone}/{business.setupTotal})</h3>
                    <ul className={styles.setup}>
                        {details.setup.map((step) => (
                            <li key={step.title}>
                                <span aria-hidden="true">{step.done ? "✅" : "⬜"}</span> {step.title}
                                <span className={styles.srOnly}>{step.done ? " (ferdig)" : " (mangler)"}</span>
                            </li>
                        ))}
                    </ul>
                </section>
            </div>

            <section className={styles.card}>
                <h2 className={styles.cardTitle}>Notat (bare for BESTI)</h2>
                <NoteForm id={business.id} note={details.adminNote ?? ""} action={noteAction}/>
            </section>
        </>
    );
}
