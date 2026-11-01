import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPanelBusiness, getServiceTemplates } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatLength, formatPrice } from "@/lib/format";
import type { Service } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import EmptyState from "@/components/EmptyState/EmptyState";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import { addServiceAction, deleteServiceAction, updateServiceAction } from "./actions";
import ServiceForm from "./ServiceForm";
import ServiceSuggestions from "./ServiceSuggestions";
import styles from "./tjenester.module.css";

export const metadata: Metadata = { title: "Tjenester" };

type ServicesPageProps = {
    searchParams: Promise<{ feil?: string; "lagt-til"?: string; endret?: string; slettet?: string; forslag?: string }>;
};

// What customers book: name and description, the length as a mint pill, the price as a crystal box,
// and Rediger / Slett tjeneste as small pills that open under the card (the same look as Ansatte)
export default async function ServicesPage({ searchParams }: ServicesPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }
    const params = await searchParams;
    const templates = await getServiceTemplates(token);
    const count = business.services.length;
    // Fixed text only: nothing from the URL is shown as a message
    const notice = params["lagt-til"] ? "Tjenesten er lagt til."
        : params.forslag ? "Tjenestene er lagt til. Du kan endre dem under."
        : params.endret ? "Tjenesten er endret."
        : params.slettet ? "Tjenesten er slettet."
        : null;

    return (
        <main className={panel.main}>
            <PanelNav title="Tjenester"/>
            <div className={styles.summary}>
                <p className={styles.count}>
                    <strong>{count}</strong> {count === 1 ? "tjeneste" : "tjenester"}
                </p>
                <p className={styles.privacy}>
                    Dette ser kundene når de bestiller: navn, hvor lenge timen varer og pris.
                </p>
            </div>
            {notice && <p className={styles.notice} role="status">{notice}</p>}
            {params.feil === "slett" && (
                <p className={styles.error} role="alert">Kunne ikke slette tjenesten.</p>
            )}

            {count === 0 ? (
                <EmptyState icon="✂️" title="Ingen tjenester ennå">
                    Uten tjenester kan ingen bestille. {templates.length > 0 ? "Legg til din egen, eller velg fra forslagene" : "Legg til din egen"} under.
                </EmptyState>
            ) : (
                <ul className={styles.cards}>
                    {business.services.map((service) => (
                        <li key={service.id} className={styles.card}>
                            <div className={styles.what}>
                                <p className={styles.name}><CrystalIcon name="stjerne"/> {service.name}</p>
                                <span className={styles.length}>
                                    <ClockIcon/> {formatLength(service.durationMinutes)}
                                </span>
                                {service.description && <p className={styles.description}>{service.description}</p>}
                            </div>
                            <Price service={service}/>
                            <div className={styles.actions}>
                                <details className={`${styles.action} ${styles.actionMint}`}>
                                    <summary><PenIcon/> Rediger</summary>
                                    <div className={styles.sheet}>
                                        <ServiceForm action={updateServiceAction} serviceId={service.id} submitText="Lagre"
                                                     initial={{
                                                         name: service.name,
                                                         description: service.description ?? "",
                                                         durationMinutes: String(service.durationMinutes),
                                                         priceType: service.priceType,
                                                         priceNok: service.priceNok === null ? "" : String(service.priceNok),
                                                     }}/>
                                    </div>
                                </details>
                                <details className={`${styles.action} ${styles.actionDanger}`}>
                                    <summary>Slett tjeneste</summary>
                                    <div className={`${styles.sheet} ${styles.sheetDanger}`}>
                                        <p>Er du sikker? Kunder kan ikke lenger bestille {service.name}.</p>
                                        <form action={deleteServiceAction}>
                                            <input type="hidden" name="serviceId" value={service.id}/>
                                            <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                                                Ja, slett
                                            </SubmitButton>
                                        </form>
                                    </div>
                                </details>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            {/* Not done every day, so only a button; the form opens under it */}
            <details className={styles.add} open={count === 0 && templates.length === 0}>
                <summary className={styles.addTitle}>
                    <span className={styles.plus} aria-hidden="true">
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                        <img src="/ikoner/iskra.svg" alt=""/>
                    </span>
                    Legg til tjeneste
                </summary>
                <ServiceForm action={addServiceAction} submitText="Legg til" className={styles.addForm}/>
            </details>

            {templates.length > 0 && (
                <ServiceSuggestions templates={templates} open={count === 0}/>
            )}
        </main>
    );
}

// "690 kr" as a crystal number; "Pris etter avtale" in words, smaller, in the same crystal colours
function Price({ service }: { service: Service }) {
    const deal = service.priceNok === null || service.priceType === "EtterAvtale";
    return (
        <div className={deal ? `${styles.price} ${styles.priceDeal}` : styles.price}>
            <span className={styles.priceValue}>
                {deal ? "Pris etter avtale" : formatPrice(service.priceNok, service.priceType)}
            </span>
        </div>
    );
}

function ClockIcon() {
    return (
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="12" cy="12" r="8"/>
            <path d="M12 8v4l3 2"/>
        </svg>
    );
}

function PenIcon() {
    return (
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"
             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M4 20h4L19 9l-4-4L4 16v4z"/>
            <path d="M14 6l4 4"/>
        </svg>
    );
}
