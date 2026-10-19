import Link from "next/link";
import { forgotPasswordAction } from "./actions";
import styles from "../logg-inn/page.module.css";

type Props = {
    searchParams: Promise<{ sendt?: string }>;
};

export default async function ForgotPasswordPage({ searchParams }: Props) {
    const { sendt } = await searchParams;
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Glemt passord</h1>
            <p className={styles.muted}>Skriv inn e-posten du logger inn med, så sender vi deg en lenke.</p>
            {sendt && (
                <p className={styles.success} role="status">
                    Hvis e-posten finnes hos oss, har vi sendt en lenke. Sjekk innboksen (og søppelpost).
                </p>
            )}
            <form action={forgotPasswordAction} className={styles.form}>
                <label className={styles.field}>
                    E-post
                    <input name="email" type="email" required autoComplete="email" />
                </label>
                <button type="submit" className={styles.primaryButton}>
                    Send lenke
                </button>
            </form>
            <p className={styles.signup}>
                <Link href="/logg-inn">Tilbake til innlogging</Link>
            </p>
        </main>
    );
}