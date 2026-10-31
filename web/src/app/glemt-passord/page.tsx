import Link from "next/link";
import { forgotPasswordAction } from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../logg-inn/page.module.css";

type Props = {
    searchParams: Promise<{ sendt?: string }>;
};

export default async function ForgotPasswordPage({ searchParams }: Props) {
    const { sendt } = await searchParams;
    return (
        <main className={styles.main}>
            <h1 className={`${styles.welcome} ${styles.centered}`}>Glemt passord?</h1>
            <p className={styles.lead}>Skriv inn e-posten din, så sender vi deg en lenke til nytt passord.</p>
            <form action={forgotPasswordAction} className={styles.form}>
                <label className={styles.field}>
                    E-post
                    <input name="email" type="email" required autoComplete="email" />
                </label>
                <SubmitButton className={`${styles.primaryButton} ${styles.loginButton}`}>
                    Send lenke
                </SubmitButton>
            </form>
            {/* Under the form, where the eye goes after Send lenke; it never says whether the account exists */}
            {sendt && (
                <p className={styles.sentBox} role="status">
                    Hvis e-posten finnes hos oss, har vi sendt en lenke. Sjekk innboksen (og søppelpost).
                </p>
            )}
            <p className={`${styles.sparkleLink} ${styles.bigLink}`}>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img src="/ikoner/iskra.svg" alt="" className={styles.sparkle}/>
                <Link href="/logg-inn">Tilbake til Logg inn</Link>
            </p>
        </main>
    );
}