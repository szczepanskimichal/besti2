import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { updateServiceAction } from "../actions";
import ServiceForm from "../ServiceForm";
import styles from "../page.module.css";

type EditServicePageProps = {
    params: Promise<{ id: string }>;
};
export default async function EditServicePage({ params }: EditServicePageProps) {

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

    return (
        <main className={styles.main}>
            <Link href="/panel/tjenester" className={styles.back}>← Tilbake til tjenester</Link>
            <h1 className={styles.title}>Rediger tjeneste</h1>
            <ServiceForm
                action={updateServiceAction}
                serviceId={service.id}
                submitText="Lagre endringer"
                initial={{
                    name: service.name,
                    description: service.description ?? "",
                    durationMinutes: String(service.durationMinutes),
                    priceType: service.priceType,
                    priceNok: service.priceNok === null ? "" : String(service.priceNok),
                }}
            />
        </main>
    );
}