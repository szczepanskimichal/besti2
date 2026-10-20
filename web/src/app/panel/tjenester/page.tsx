import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatPrice } from "@/lib/format";
import { priceTypeOptions } from "@/lib/priceTypes";
import { addServiceAction, deleteServiceAction } from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";


type ServicesPageProps = {
    searchParams: Promise<{ feil?: string; "lagt-til"?: string;endret?: string; 
        slettet?: string }>;
};

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
    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Tjenester</h1>
            <p className={styles.muted}>{business.name}</p>
            
            {params.feil && (
                <p className={styles.error} role="alert">
                    {params.feil}
                </p>
            )}
            
            {params["lagt-til"] && (
                <p className={styles.success} role="status">
                    Tjenesten er lagt til.
                </p>
            )}

            {params.endret && (
                <p className={styles.success} role="status">
                    Tjenesten er endret.
                </p>
            )}

            {params.slettet && (
                <p className={styles.success} role="status">
                    Tjenesten er slettet.
                </p>
            )}
            
            {business.services.length === 0 ? (
                <p className={styles.muted}>Ingen tjenester ennå. Legg til den første under.</p>
            ) : (
                <ul className={styles.list}>
                    {business.services.map((service) => (
                        <li key={service.id} className={styles.card}>
                            <p className={styles.serviceName}>{service.name}</p>
                            {service.description && (
                                <p className={styles.muted}>{service.description}</p>
                            )}
                            <p className={styles.muted}>
                                {formatPrice(service.priceNok, service.priceType)} · timen varer {service.durationMinutes} min
                            </p>
                            <div className={styles.cardActions}>
                                <Link href={`/panel/tjenester/${service.id}`} className={styles.secondaryButton}>
                                    Rediger
                                </Link>
                                <details className={styles.deleteBox}>
                                    <summary>Slett</summary>
                                    <p>Er du sikker? Kunder kan ikke lenger bestille denne tjenesten.</p>
                                    <form action={deleteServiceAction}>
                                        <input type="hidden" name="serviceId" value={service.id} />
                                        <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                                            Ja, slett
                                        </SubmitButton>
                                    </form>
                                </details>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            <h2 className={styles.subtitle}>Legg til tjeneste</h2>

            <form action={addServiceAction} className={styles.form}>
                <label className={styles.field}>
                    Navn
                    <input name="name" required maxLength={200} />
                </label>

                <label className={styles.field}>
                    Beskrivelse (valgfritt)
                    <input name="description" maxLength={500} />
                </label>

                <label className={styles.field}>
                    Hvor lenge varer timen? (minutter)
                    <input name="durationMinutes" type="number" required min={5} max={480} step={5} defaultValue={30} />
                </label>

                <label className={styles.field}>
                    Pristype
                    <select name="priceType" required defaultValue="FastPris">
                        {priceTypeOptions.map((option) => (
                            <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                    </select>
                </label>

                <label className={styles.field}>
                    Pris i NOK (tom ved pris etter avtale)
                    <input name="priceNok" type="number" min={0} step={1} />
                </label>

                <SubmitButton className={styles.primaryButton}>
                    Legg til
                </SubmitButton>
            </form>
        </main>
    );
}
