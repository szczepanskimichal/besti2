import { notFound, redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { updateServiceAction } from "../actions";
import ServiceForm from "../ServiceForm";
import PanelNav from "../../PanelNav";
import panel from "../../page.module.css";
import styles from "../tjenester.module.css";

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
        <main className={panel.main}>
            <PanelNav title="Rediger tjeneste" active="Tjenester"/>
            <div className={`${styles.sheet} ${styles.editSheet}`}>
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
            </div>
        </main>
    );
}