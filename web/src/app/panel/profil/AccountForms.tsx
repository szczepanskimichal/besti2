"use client";
import { useActionState } from "react";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { type AccountFormState, changePasswordAction, deleteBusinessAction } from "./actions";
import styles from "../tjenester/page.module.css";

// Password and "Slett bedrift" as two separate forms, so an error in one never empties the other
export default function AccountForms() {
    const [password, passwordAction] = useActionState<AccountFormState, FormData>(changePasswordAction, {});
    const [deletion, deleteAction] = useActionState<AccountFormState, FormData>(deleteBusinessAction, {});

    return (
        <>
            <h2 className={styles.subtitle}>Endre passord</h2>
            <form action={passwordAction} className={styles.form}>
                {password.error && <p className={styles.error} role="alert">{password.error}</p>}
                {password.saved && <p className={styles.success} role="status">Passordet er endret.</p>}
                <label className={styles.field}>
                    Nåværende passord
                    <input name="currentPassword" type="password" required autoComplete="current-password" />
                </label>
                <label className={styles.field}>
                    Nytt passord (minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn)
                    <input name="newPassword" type="password" required minLength={6} autoComplete="new-password" />
                </label>
                <label className={styles.field}>
                    Gjenta nytt passord
                    <input name="repeatPassword" type="password" required minLength={6} autoComplete="new-password" />
                </label>
                <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Endre passord</SubmitButton>
            </form>

            <h2 className={styles.subtitle}>Slett bedrift</h2>
            <div className={styles.card}>
                <p>
                    Bedriften, tjenestene, de ansatte, bildene, alle timer og vurderinger og innloggingen din blir
                    slettet. Har dere kommende timer, må dere avlyse dem i kalenderen først, så får kundene beskjed.
                    Dette kan ikke angres.
                </p>
                <details className={styles.deleteBox}>
                    <summary>Slett bedriften</summary>
                    <form action={deleteAction} className={styles.form}>
                        {deletion.error && <p className={styles.error} role="alert">{deletion.error}</p>}
                        <label className={styles.field}>
                            Skriv passordet ditt for å bekrefte
                            <input name="password" type="password" required autoComplete="current-password" />
                        </label>
                        <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                            Ja, slett bedriften
                        </SubmitButton>
                    </form>
                </details>
            </div>
        </>
    );
}
