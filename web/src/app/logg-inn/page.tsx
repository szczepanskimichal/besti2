import type { Metadata } from "next";
import { cookies } from "next/headers";
import Link from "next/link";
import { privatePage } from "@/lib/site";
import GoogleButton from "@/components/GoogleButton/GoogleButton";
import LoginForm from "./LoginForm";
import { GOOGLE_ERROR_COOKIE } from "./google/googleLogin";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Logg inn", robots: privatePage };

type LoginPageProps = {
    searchParams: Promise<{ registrert?: string, nyttPassord?: string, neste?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
    const { registrert, nyttPassord, neste } = await searchParams;
    // Set for one minute by "Fortsett med Google" when it failed
    const googleError = (await cookies()).get(GOOGLE_ERROR_COOKIE)?.value;

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Logg inn</h1>
            <p className={styles.muted}>For kunder og bedrifter.</p>
            {registrert && (
                <p className={styles.success} role="status">
                    Bedriften er registrert. Sjekk e-posten din og klikk på lenken for å bekrefte, så kan du logge inn.
                    Før kundene ser bedriften, sjekker vi den – vanligvis innen én arbeidsdag.
                </p>
            )}
            {nyttPassord && (
                <p className={styles.success} role="status">
                    Passordet er endret. Logg inn med det nye passordet.
                </p>
            )}
            {googleError && <p className={styles.error} role="alert">{googleError}</p>}
            <GoogleButton next={neste}/>
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