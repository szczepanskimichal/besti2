import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getToken } from "@/lib/auth";
import PanelNav from "../../PanelNav";
import panel from "../../page.module.css";
import styles from "../page.module.css";
import ImportCustomers from "./ImportCustomers";

export const metadata: Metadata = { title: "Importer kunder" };

// A customer list from the old system (Timma, Fresha, Timely, Excel...) into Kunder. The file is read in the
// browser, the business chooses the columns, BESTI checks the list, and nobody gets an e-mail or SMS
export default async function ImportCustomersPage() {
    if (!(await getToken())) {
        redirect("/logg-inn");
    }

    return (
        <main className={panel.main}>
            <PanelNav title="Importer kunder"/>
            <Link href="/panel/kunder" className={styles.back}>← Alle kunder</Link>
            <div className={styles.importFlow}>
                <section className={panel.card} aria-labelledby="slik">
                    <h2 id="slik" className={styles.cardTitle}>Slik flytter dere kundelisten</h2>
                    <ol className={styles.steps}>
                        <li>Eksporter kundene fra systemet dere bruker i dag, som en CSV-fil. Ofte ligger det under
                            «Kunder» → «Eksporter».</li>
                        <li>Har dere en Excel-fil, velg «Fil» → «Lagre som» → «CSV».</li>
                        <li>Velg filen her, og fortell oss hvilken kolonne som er navn, telefon og e-post.</li>
                    </ol>
                    <p className={styles.noteHint}>
                        Kundene får ingen e-post eller SMS fra BESTI. De dukker opp under Kunder, og blir koblet til
                        bestillingene sine når de bestiller med samme telefonnummer.
                    </p>
                </section>
                <ImportCustomers/>
            </div>
        </main>
    );
}
