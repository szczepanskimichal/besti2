import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { priceTypeOptions } from "@/lib/priceTypes";
import { updateServiceAction } from "../actions";
import styles from "../page.module.css";

type EditServicePageProps = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ feil?: string }>;
};

export default async function EditServicePage({ params, searchParams }: EditServicePageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }

    const { id } = await params;
    const service = business.services.find((s) => s.id === id);
    if (!service) {
        notFound();
    }

    const { feil } = await searchParams;

    return (
        <main className={styles.main}>
            <Link href="/panel/tjenester" className={styles.back}>← Tilbake til tjenester</Link>
            <h1 className={styles.title}>Rediger tjeneste</h1>

            {feil && (
                <p className={styles.error} role="alert">
                    {feil}
                </p>
            )}

            <form action={updateServiceAction} className={styles.form}>
                <input type="hidden" name="serviceId" value={service.id} />

                <label className={styles.field}>
                    Navn
                    <input name="name" required maxLength={200} defaultValue={service.name} />
                </label>

                <label className={styles.field}>
                    Beskrivelse (valgfritt)
                    <input name="description" maxLength={500} defaultValue={service.description ?? ""} />
                </label>

                <label className={styles.field}>
                    Hvor lenge varer timen? (minutter)
                    <input name="durationMinutes" type="number" required min={5} max={480} step={5} defaultValue={service.durationMinutes} />
                </label>

                <label className={styles.field}>
                    Pristype
                    <select name="priceType" required defaultValue={service.priceType}>
                        {priceTypeOptions.map((option) => (
                            <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                    </select>
                </label>

                <label className={styles.field}>
                    Pris i NOK (tom ved pris etter avtale)
                    <input name="priceNok" type="number" min={0} step={1} defaultValue={service.priceNok ?? ""} />
                </label>

                <button type="submit" className={styles.primaryButton}>
                    Lagre endringer
                </button>
            </form>
        </main>
    );
}
