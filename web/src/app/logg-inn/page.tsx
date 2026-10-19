import Link from "next/link";
import { loginAction } from "./actions";
import styles from "./page.module.css";

type LoginPageProps = {
    searchParams: Promise<{ feil?: string, registrert?: string, nyttPassord?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
    const { feil, registrert, nyttPassord } = await searchParams;

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Logg inn</h1>
            <p className={styles.muted}>For bedrifter som bruker besti2.</p>
            {feil && (
                <p className={styles.error} role="alert">
                    Feil e-post eller passord. Prøv igjen.
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
                <label className={styles.field}>
                    E-post
                    <input name="email" type="email" required autoComplete="email" />
                </label>
                <label className={styles.field}>
                    Passord
                    <input name="password" type="password" required autoComplete="current-password" />
                </label>
                <button type="submit" className={styles.primaryButton}>
                    Logg inn
                </button>
            </form>
            <p className={styles.signup}>
                Ny her? <Link href="/registrer-bedrift">Registrer bedriften din</Link>
            </p>
            <p className={styles.signup}>
                <Link href="/glemt-passord">Glemt passord?</Link>
            </p>
        </main>
    );
}
