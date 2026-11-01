import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getPanelBusiness, getStaffAccess } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { StaffAccess } from "@/lib/types";
import PanelNav from "../PanelNav";
import panel from "../page.module.css";
import { addEmployeeAction, updateEmployeeAction } from "./actions";
import EmployeeForm from "./EmployeeForm";
import DeleteEmployeeForm from "./DeleteEmployeeForm";
import StaffAccessBox from "./StaffAccessBox";
import styles from "./ansatte.module.css";

export const metadata: Metadata = { title: "Ansatte" };

type EmployeesPageProps = {
    searchParams: Promise<{ "lagt-til"?: string; endret?: string; slettet?: string; invitert?: string; "tilgang-fjernet"?: string }>;
};

// The people customers can book with: crystal initials, the login status as a pill (mint = has own login,
// lilac = invitation waiting) and Rediger / Egen innlogging / Slett as small pills that open under the card
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
    const count = business.employees.length;
    const notice = params["lagt-til"] ? "Den ansatte er lagt til."
        : params.endret ? "Den ansatte er endret."
        : params.invitert ? "Lenken er sendt. Den ansatte får en e-post (lenken virker i 7 dager)."
        : params["tilgang-fjernet"] ? "Tilgangen er fjernet."
        : params.slettet ? "Den ansatte er slettet."
        : null;

    return (
        <main className={panel.main}>
            <PanelNav title="Ansatte"/>
            <div className={styles.summary}>
                <p className={styles.count}>
                    <strong>{count}</strong> {count === 1 ? "ansatt" : "ansatte"}
                </p>
                <p className={styles.privacy}>
                    <LockIcon size={17}/>
                    Med tilgang får den ansatte sin egen jobbkalender: bare egne timer, ikke priser eller statistikk.
                </p>
            </div>
            {notice && <p className={styles.notice} role="status">{notice}</p>}

            <ul className={styles.cards}>
                {business.employees.map((employee) => {
                    const login = access.find((a) => a.employeeId === employee.id);
                    return (
                        <li key={employee.id} className={styles.card}>
                            <span className={styles.avatar} aria-hidden="true">
                                {initials(`${employee.name} ${employee.lastName ?? ""}`)}
                            </span>
                            <div className={styles.who}>
                                <p className={styles.name}>{employee.name} {employee.lastName}</p>
                                <LoginStatus login={login}/>
                            </div>
                            <div className={styles.actions}>
                                <details className={`${styles.action} ${styles.actionMint}`}>
                                    <summary><PenIcon/> Rediger</summary>
                                    <div className={styles.sheet}>
                                        <EmployeeForm action={updateEmployeeAction} employeeId={employee.id}
                                                      name={employee.name} lastName={employee.lastName} submitText="Lagre"/>
                                    </div>
                                </details>
                                <details className={`${styles.action} ${styles.actionMint}`}>
                                    <summary><LockIcon/> Tilgang</summary>
                                    <div className={styles.sheet}>
                                        <StaffAccessBox employeeId={employee.id} name={employee.name} access={login}/>
                                    </div>
                                </details>
                                {!isLast && (
                                    <details className={`${styles.action} ${styles.actionDanger}`}>
                                        <summary>Slett ansatt</summary>
                                        <div className={`${styles.sheet} ${styles.sheetDanger}`}>
                                            <p>Er du sikker? Kunder kan ikke lenger bestille time hos {employee.name}.</p>
                                            <DeleteEmployeeForm employeeId={employee.id}/>
                                        </div>
                                    </details>
                                )}
                            </div>
                        </li>
                    );
                })}
            </ul>
            {isLast && (
                <p className={styles.hint}>Bedriften må ha minst én ansatt, så den siste kan ikke slettes.</p>
            )}

            {/* Not done every day, so only a button; the form opens under it */}
            <details className={styles.add}>
                <summary className={styles.addTitle}>
                    <span className={styles.plus} aria-hidden="true">+</span>
                    Legg til ansatt
                </summary>
                <EmployeeForm action={addEmployeeAction} submitText="Legg til" className={styles.addForm}/>
            </details>
            <p className={styles.hint}>
                «Tilgang»: den ansatte logger inn med sin egen BESTI-konto og finner jobbkalenderen sin under «Jobb»,
                med egne timer, kundenavn og notater.
            </p>
        </main>
    );
}

// Under the name, always with the lock: mint = has access, lilac = waiting for the employee, dashed = no access
function LoginStatus({ login }: { login: StaffAccess | undefined }) {
    if (login?.connected) {
        return <span className={`${styles.status} ${styles.statusOn}`}><LockIcon/> <span>Har tilgang</span></span>;
    }
    if (login?.email) {
        return <span className={`${styles.status} ${styles.statusWaiting}`}><LockIcon/> <span>Venter på tilgang</span></span>;
    }
    return <span className={styles.status}><LockIcon/> <span>Ingen tilgang</span></span>;
}

function LockIcon({ size = 14 }: { size?: number }) {
    return (
        <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <rect x="5" y="11" width="14" height="9" rx="2"/>
            <path d="M8 11V8a4 4 0 0 1 8 0v3"/>
        </svg>
    );
}

function PenIcon() {
    return (
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"
             strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M4 20h4L19 9l-4-4L4 16v4z"/>
            <path d="M14 6l4 4"/>
        </svg>
    );
}

// "Ida Hansen" -> "IH", "Kari" -> "K"
function initials(name: string) {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part.charAt(0).toUpperCase()).join("");
}
