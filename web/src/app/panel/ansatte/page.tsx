import Link from "next/link";
import { redirect } from "next/navigation";
import { getPanelBusiness, getStaffAccess } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { addEmployeeAction, updateEmployeeAction } from "./actions";
import EmployeeForm from "./EmployeeForm";
import DeleteEmployeeForm from "./DeleteEmployeeForm";
import StaffAccessBox from "./StaffAccessBox";
import styles from "../tjenester/page.module.css";

type EmployeesPageProps = {
    searchParams: Promise<{ "lagt-til"?: string; endret?: string; slettet?: string; invitert?: string; "tilgang-fjernet"?: string }>;
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
    const access = await getStaffAccess(token);
    const isLast = business.employees.length <= 1;
    return (
        <main className={styles.main}>
            <Link href="/panel" className={styles.back}>← Tilbake til bestillinger</Link>
            <h1 className={styles.title}>Ansatte</h1>
            <p className={styles.muted}>{business.name}</p>
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
            {params.invitert && (
                <p className={styles.success} role="status">
                    Invitasjonen er sendt. Den ansatte får en e-post med lenke (gyldig i 7 dager).
                </p>
            )}
            {params["tilgang-fjernet"] && (
                <p className={styles.success} role="status">
                    Tilgangen er fjernet.
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
                            {access.some((a) => a.employeeId === employee.id && a.connected) && (
                                <span className={styles.muted}> · har egen innlogging</span>
                            )}
                        </p>
                        <div className={styles.cardActions}>
                            <details className={styles.editBox}>
                                <summary>Rediger</summary>
                                <EmployeeForm action={updateEmployeeAction} employeeId={employee.id}
                                              name={employee.name} lastName={employee.lastName} submitText="Lagre" />
                            </details>
                            <details className={styles.editBox}>
                                <summary>Egen innlogging</summary>
                                <StaffAccessBox employeeId={employee.id} name={employee.name}
                                                access={access.find((a) => a.employeeId === employee.id)}/>
                            </details>
                            {!isLast && (
                                <details className={styles.deleteBox}>
                                    <summary>Slett</summary>
                                    <p>Er du sikker? Kunder kan ikke lenger bestille time hos denne personen.</p>
                                    <DeleteEmployeeForm employeeId={employee.id} />
                                </details>
                            )}
                        </div>
                    </li>
                ))}
            </ul>
            {isLast && (
                <p className={styles.muted}>Bedriften må ha minst én ansatt, så den siste kan ikke slettes.</p>
            )}
            <p className={styles.muted}>
                «Egen innlogging»: den ansatte ser sine egne timer, kundenavn og notater på bestiapp.no/ansatt med
                sin egen BESTI-konto – ikke priser, statistikk eller innstillinger.
            </p>
            <h2 className={styles.subtitle}>Legg til ansatt</h2>
            <EmployeeForm action={addEmployeeAction} submitText="Legg til" />
        </main>
    );
}