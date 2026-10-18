import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { weekdays } from "@/lib/weekdays";
import { updateOpeningHoursAction } from "./actions";
import styles from "../tjenester/page.module.css";
import dayStyles from "./page.module.css";

type OpeningHoursPageProps = {
    searchParams: Promise<{ feil?: string; lagret?: string }>;
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

    const { feil, lagret } = await searchParams;

    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Åpningstider</h1>
            <p className={styles.muted}>Kryss av dagene dere har åpent. Kunder kan bare bestille innenfor disse tidene.</p>

            {feil && (
                <p className={styles.error} role="alert">
                    {feil}
                </p>
            )}

            {lagret && (
                <p className={styles.success} role="status">
                    Åpningstidene er lagret.
                </p>
            )}

            <form action={updateOpeningHoursAction} className={styles.form}>
                {weekdays.map((w) => {
                    // undefined = closed this day (no row in the database)
                    const hours = business.openingHours.find((h) => h.dayOfWeek === w.day);

                    return (
                        <fieldset key={w.day} className={dayStyles.dayRow}>
                            <legend>{w.label}</legend>

                            <label className={dayStyles.checkbox}>
                                <input type="checkbox" name={`open-${w.day}`} defaultChecked={hours !== undefined} />
                                Åpen
                            </label>

                            <label className={styles.field}>
                                Fra
                                <input type="time" name={`opensAt-${w.day}`} defaultValue={hours?.opensAt.slice(0, 5) ?? "09:00"} />
                            </label>

                            <label className={styles.field}>
                                Til
                                <input type="time" name={`closesAt-${w.day}`} defaultValue={hours?.closesAt.slice(0, 5) ?? "17:00"} />
                            </label>
                        </fieldset>
                    );
                })}

                <button type="submit" className={styles.primaryButton}>
                    Lagre åpningstider
                </button>
            </form>
        </main>
    );
}

