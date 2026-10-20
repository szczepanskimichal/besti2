import Link from "next/link";
import { loginAction } from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type LoginPageProps = {
    searchParams: Promise<{ feil?: string, registrert?: string, nyttPassord?: string, neste?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
    const { feil, registrert, nyttPassord, neste } = await searchParams;

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Logg inn</h1>
            <p className={styles.muted}>For kunder og bedrifter.</p>
            {feil && (
                <p className={styles.error} role="alert">
                    {feil === "for-mange"
                        ? "For mange forsøk. Prøv igjen om litt."
                        : "Feil e-post eller passord. Har du nettopp opprettet konto? Bekreft e-posten først."}
                </p>
            )}
            {registrert && (
                <p className={styles.success} role="status">
                    Bedriften er registrert. Logg inn med e-post og passord.
                </p>
            )}
            {nyttPassord && (
                <p className={styles.success} role="status">
                    Passordet er endret. Logg inn med det nye passordet.
                </p>
            )}
            <form action={loginAction} className={styles.form}>
                <input type="hidden" name="neste" value={neste ?? ""} />
                <label className={styles.field}>
                    E-post
                    <input name="email" type="email" required autoComplete="email" />
                </label>
                <label className={styles.field}>
                    Passord
                    <input name="password" type="password" required autoComplete="current-password" />
                </label>
                <SubmitButton className={styles.primaryButton} pendingText="Logger inn…">
                    Logg inn
                </SubmitButton>
            </form>
            <p className={styles.signup}>
                Ny kunde? <Link href="/registrer">Opprett konto</Link>
            </p>
            <p className={styles.signup}>
                Har du bedrift? <Link href="/registrer-bedrift">Registrer bedriften din</Link>
            </p>
            <p className={styles.signup}>
                <Link href="/glemt-passord">Glemt passord?</Link>
            </p>
        </main>
    );
}
