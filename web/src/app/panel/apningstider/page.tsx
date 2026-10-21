import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { weekdays } from "@/lib/weekdays";
import OpeningHoursForm from "./OpeningHoursForm";
import styles from "../tjenester/page.module.css";

type OpeningHoursPageProps = {
    searchParams: Promise<{ lagret?: string }>;
};

export default async function OpeningHoursPage({ searchParams }: OpeningHoursPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }

    const { lagret } = await searchParams;

    // The saved hours as form values; a day without a row in the database is closed
    const initial: Record<string, string> = {};
    for (const w of weekdays) {
        const hours = business.openingHours.find((h) => h.dayOfWeek === w.day);
        initial[`open-${w.day}`] = hours ? "on" : "";
        initial[`opensAt-${w.day}`] = hours?.opensAt.slice(0, 5) ?? "09:00";
        initial[`closesAt-${w.day}`] = hours?.closesAt.slice(0, 5) ?? "17:00";
    }

    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Åpningstider</h1>
            <p className={styles.muted}>Kryss av dagene dere har åpent. Kunder kan bare bestille innenfor disse tidene.</p>

            {lagret && (
                <p className={styles.success} role="status">
                    Åpningstidene er lagret.
                </p>
            )}

            <OpeningHoursForm initial={initial} />
        </main>
    );
}
