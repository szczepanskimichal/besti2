import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminBusiness, getAdminReasons } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate, formatDateTime } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { decideAction, noteAction, suspendAction } from "../../actions";
import NoteForm from "./NoteForm";
import styles from "../../admin.module.css";

export const metadata: Metadata = { title: "Bedrift" };

type AdminBusinessPageProps = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ avvis?: string; lagret?: string; godkjent?: string; feil?: string; endret?: string; steng?: string }>;
};

// One business: contact details for a call, what is missing in "Kom i gang", and BESTI's own note
export default async function AdminBusinessPage({ params, searchParams }: AdminBusinessPageProps) {
    const { id } = await params;
    const query = await searchParams;
    const token = await getToken();
    const [details, reasons] = token && /^[0-9a-f-]{36}$/i.test(id)
        ? await Promise.all([getAdminBusiness(token, id), getAdminReasons(token)])
        : [null, null];
    if (!details || !reasons) {
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
            {query.endret && <p className={styles.notice} role="status">Lagret. Eieren har fått beskjed.</p>}
            {query.feil && <p className={styles.error} role="alert">{query.feil}</p>}
            {details.suspendedUtc && (
                <p className={styles.error} role="status">
                    Stengt av BESTI {formatDate(details.suspendedUtc)}: {details.suspendedReason}
                </p>
            )}

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

            {(business.status === "Synlig" || business.status === "Stengt") && (
                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>{business.status === "Stengt" ? "Åpne siden igjen" : "Steng siden"}</h2>
                    {business.status === "Stengt" ? (
                        <form action={suspendAction} className={styles.reasonForm}>
                            <input type="hidden" name="id" value={business.id}/>
                            <input type="hidden" name="open" value="1"/>
                            <SubmitButton className={styles.primaryButton} pendingText="Åpner …">Åpne igjen</SubmitButton>
                        </form>
                    ) : query.steng ? (
                        <>
                            <p>
                                Kundene finner ikke siden og kan ikke bestille nye timer. Timer som allerede er
                                bestilt, gjelder fortsatt ({business.upcoming} kommende). Eieren får e-post med grunnen
                                og kan bruke panelet som før. Det kan angres med «Åpne igjen».
                            </p>
                            <form action={suspendAction} className={styles.reasonForm}>
                                <input type="hidden" name="id" value={business.id}/>
                                <label className={styles.srOnly} htmlFor="steng-grunn">Grunn</label>
                                <select id="steng-grunn" name="reason" required defaultValue="" className={styles.select}>
                                    <option value="" disabled>Velg grunn …</option>
                                    {reasons.suspend.map((reason) => <option key={reason} value={reason}>{reason}</option>)}
                                </select>
                                <SubmitButton className={styles.dangerButton} pendingText="Stenger …">Steng siden</SubmitButton>
                                <Link href={`/admin/bedrifter/${business.id}`} className={styles.secondaryButton}>Avbryt</Link>
                            </form>
                        </>
                    ) : (
                        <>
                            <p className={styles.muted}>For falske bedrifter, mange klager, eller når eieren ber om det.</p>
                            <Link href={`/admin/bedrifter/${business.id}?steng=1`} className={styles.dangerButton}>Steng siden …</Link>
                        </>
                    )}
                </section>
            )}

            {details.log && details.log.length > 0 && (
                <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Logg</h2>
                    <ul className={styles.log}>
                        {details.log.map((entry) => (
                            <li key={`${entry.createdUtc}-${entry.text}`}>
                                <span>{entry.text}</span>
                                <span className={styles.muted}>{formatDateTime(entry.createdUtc)} · {entry.adminEmail}</span>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
        </>
    );
}
