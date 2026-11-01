import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { weekdays } from "@/lib/weekdays";
import PanelNav from "../PanelNav";
import OpeningHoursForm from "./OpeningHoursForm";
import panel from "../page.module.css";
import styles from "./apningstider.module.css";

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
        // One break per day (row 0)
        initial[`breakStart-${w.day}-0`] = hours?.breakStart?.slice(0, 5) ?? "";
        initial[`breakEnd-${w.day}-0`] = hours?.breakEnd?.slice(0, 5) ?? "";
    }

    return (
        <main className={panel.main}>
            <PanelNav title="Åpningstider"/>
            <p className={styles.lead}>
                Kunder kan bare bestille innenfor disse tidene. Har dere lunsj eller en fast pause,
                fyll ut pausen: da kan ingen bestille den tiden.
            </p>

            {lagret && (
                <p className={styles.notice} role="status">
                    Åpningstidene er lagret.
                </p>
            )}

            <OpeningHoursForm initial={initial}/>
        </main>
    );
}
