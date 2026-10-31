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
            <h1 className={styles.welcome}>Velkommen til BESTI</h1>
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
            <p className={styles.sparkleLink}>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img src="/ikoner/iskra.svg" alt="" className={styles.sparkle}/>
                Ny på BESTI? <Link href={`/registrer?neste=${encodeURIComponent(neste ?? "/")}`}>Opprett konto</Link>
            </p>
            <p className={styles.sparkleLink}>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img src="/ikoner/iskra.svg" alt="" className={styles.sparkle}/>
                Har du bedrift? <Link href="/registrer-bedrift">Registrer her</Link>
            </p>
        </main>
    );
}