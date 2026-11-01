"use client";
import { useActionState } from "react";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { type AccountFormState, changePasswordAction, deleteBusinessAction } from "./actions";
import styles from "./profil.module.css";

// Password and "Slett bedrift" as two separate forms, so an error in one never empties the other.
// Not done every day: both sit behind a small pill and open under it
export default function AccountForms() {
    const [password, passwordAction] = useActionState<AccountFormState, FormData>(changePasswordAction, {});
    const [deletion, deleteAction] = useActionState<AccountFormState, FormData>(deleteBusinessAction, {});

    return (
        <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Konto</h2>
            {password.saved && <p className={styles.notice} role="status">Passordet er endret.</p>}
            <div className={styles.accountRow}>
                <details className={styles.more} open={!!password.error}>
                    <summary className={styles.pill}>Endre passord</summary>
                    <form action={passwordAction} className={styles.sheet}>
                        {password.error && <p className={styles.error} role="alert">{password.error}</p>}
                        <label className={styles.field}>
                            <span>Nåværende passord</span>
                            <input name="currentPassword" type="password" required autoComplete="current-password"/>
                        </label>
                        <label className={styles.field}>
                            <span>Nytt passord (minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn)</span>
                            <input name="newPassword" type="password" required minLength={6} autoComplete="new-password"/>
                        </label>
                        <label className={styles.field}>
                            <span>Gjenta nytt passord</span>
                            <input name="repeatPassword" type="password" required minLength={6} autoComplete="new-password"/>
                        </label>
                        <SubmitButton className={styles.saveButton} pendingText="Lagrer…">Endre passord</SubmitButton>
                    </form>
                </details>

                {/* Slett bedrift: the blue Avbestill pill, with the warning and the password inside */}
                <details className={styles.more} open={!!deletion.error}>
                    <summary className={`${styles.pill} ${styles.pillDanger}`}>Slett bedrift</summary>
                    <div className={`${styles.sheet} ${styles.sheetDanger}`}>
                        <p className={styles.text}>
                            Bedriften, tjenestene, de ansatte, bildene, alle timer og vurderinger og innloggingen din
                            blir slettet. Har dere kommende timer, må dere avlyse dem i kalenderen først, så får
                            kundene beskjed. Dette kan ikke angres.
                        </p>
                        <form action={deleteAction} className={styles.sheetForm}>
                            {deletion.error && <p className={styles.error} role="alert">{deletion.error}</p>}
                            <label className={styles.field}>
                                <span>Skriv passordet ditt for å bekrefte</span>
                                <input name="password" type="password" required autoComplete="current-password"/>
                            </label>
                            <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                                Ja, slett bedriften
                            </SubmitButton>
                        </form>
                    </div>
                </details>
            </div>
        </section>
    );
}
