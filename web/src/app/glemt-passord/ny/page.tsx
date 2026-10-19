import Link from "next/link";
import { resetPasswordAction } from "../actions";
import styles from "../../logg-inn/page.module.css";

type Props = {
    searchParams: Promise<{ epost?: string; kode?: string; feil?: string }>;
};

export default async function NewPasswordPage({ searchParams }: Props) {
    const { epost, kode, feil } = await searchParams;
    if (!epost || !kode) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Ugyldig lenke</h1>
                <p className={styles.muted}>Lenken mangler informasjon. Be om en ny lenke.</p>
                <p className={styles.signup}>
                    <Link href="/glemt-passord">Glemt passord</Link>
                </p>
            </main>
        );
    }
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Nytt passord</h1>
            <p className={styles.muted}>For {epost}</p>
            {feil && (
                <p className={styles.error} role="alert">
                    Lenken er utløpt eller passordet oppfyller ikke kravene. Prøv igjen eller be om en ny lenke.
                </p>
            )}
            <form action={resetPasswordAction} className={styles.form}>
                <input type="hidden" name="email" value={epost} />
                <input type="hidden" name="code" value={kode} />
                <label className={styles.field}>
                    Nytt passord
                    <input name="password" type="password" required minLength={6} autoComplete="new-password" />
                </label>
                <p className={styles.muted}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
                <button type="submit" className={styles.primaryButton}>
                    Lagre nytt passord
                </button>
            </form>
        </main>
    );
}