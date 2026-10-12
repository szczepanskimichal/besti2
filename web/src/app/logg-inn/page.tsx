import { loginAction } from "./actions";
import styles from "./page.module.css";

type LoginPageProps = {
    searchParams: Promise<{ feil?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
    const { feil } = await searchParams;

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Logg inn</h1>
            <p className={styles.muted}>For bedrifter som bruker besti2.</p>

            {feil && (
                <p className={styles.error} role="alert">
                    Feil e-post eller passord. Prøv igjen.
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
        </main>
    );
}
