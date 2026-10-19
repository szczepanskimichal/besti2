import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { addEmployeeAction, deleteEmployeeAction, updateEmployeeAction } from "./actions";
import styles from "../tjenester/page.module.css";

type EmployeesPageProps = {
    searchParams: Promise<{ feil?: string; "lagt-til"?: string; endret?: string; slettet?: string }>;
};

export default async function EmployeesPage({ searchParams }: EmployeesPageProps) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const business = await getPanelBusiness(token);
    if (!business) {
        redirect("/logg-inn");
    }
    const params = await searchParams;
    const isLast = business.employees.length <= 1;
    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Ansatte</h1>
            <p className={styles.muted}>{business.name}</p>
            {params.feil && (
                <p className={styles.error} role="alert">
                    {params.feil}
                </p>
            )}
            {params["lagt-til"] && (
                <p className={styles.success} role="status">
                    Den ansatte er lagt til.
                </p>
            )}
            {params.endret && (
                <p className={styles.success} role="status">
                    Den ansatte er endret.
                </p>
            )}
            {params.slettet && (
                <p className={styles.success} role="status">
                    Den ansatte er slettet.
                </p>
            )}
            <ul className={styles.list}>
                {business.employees.map((employee) => (
                    <li key={employee.id} className={styles.card}>
                        <p className={styles.serviceName}>
                            {employee.name} {employee.lastName}
                        </p>
                        <div className={styles.cardActions}>
                            <details className={styles.deleteBox}>
                                <summary>Rediger</summary>
                                <form action={updateEmployeeAction} className={styles.form}>
                                    <input type="hidden" name="employeeId" value={employee.id} />
                                    <label className={styles.field}>
                                        Fornavn
                                        <input name="name" required maxLength={200} defaultValue={employee.name} />
                                    </label>
                                    <label className={styles.field}>
                                        Etternavn (valgfritt)
                                        <input name="lastName" maxLength={200} defaultValue={employee.lastName} />
                                    </label>
                                    <button type="submit" className={styles.primaryButton}>
                                        Lagre
                                    </button>
                                </form>
                            </details>
                            {!isLast && (
                                <details className={styles.deleteBox}>
                                    <summary>Slett</summary>
                                    <p>Er du sikker? Kunder kan ikke lenger bestille time hos denne personen.</p>
                                    <form action={deleteEmployeeAction}>
                                        <input type="hidden" name="employeeId" value={employee.id} />
                                        <button type="submit" className={styles.dangerButton}>
                                            Ja, slett
                                        </button>
                                    </form>
                                </details>
                            )}
                        </div>
                    </li>
                ))}
            </ul>
            {isLast && (
                <p className={styles.muted}>Bedriften må ha minst én ansatt, så den siste kan ikke slettes.</p>
            )}
            <h2 className={styles.subtitle}>Legg til ansatt</h2>
            <form action={addEmployeeAction} className={styles.form}>
                <label className={styles.field}>
                    Fornavn
                    <input name="name" required maxLength={200} />
                </label>
                <label className={styles.field}>
                    Etternavn (valgfritt)
                    <input name="lastName" maxLength={200} />
                </label>
                <button type="submit" className={styles.primaryButton}>
                    Legg til
                </button>
            </form>
        </main>
    );
}