import type { Metadata } from "next";
import Link from "next/link";
import { privatePage } from "@/lib/site";
import LoginForm from "./LoginForm";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Logg inn", robots: privatePage };

type LoginPageProps = {
    searchParams: Promise<{ registrert?: string, nyttPassord?: string, neste?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
    const { registrert, nyttPassord, neste } = await searchParams;

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Logg inn</h1>
            <p className={styles.muted}>For kunder og bedrifter.</p>
            {registrert && (
                <p className={styles.success} role="status">
                    Bedriften er registrert. Sjekk e-posten din og klikk på lenken for å bekrefte, så kan du logge inn.
                </p>
            )}
            {nyttPassord && (
                <p className={styles.success} role="status">
                    Passordet er endret. Logg inn med det nye passordet.
                </p>
            )}
            <LoginForm neste={neste ?? ""} />
            <p className={styles.signup}>
                Ny kunde? <Link href={`/registrer?neste=${encodeURIComponent(neste ?? "/")}`}>Opprett konto</Link>
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
