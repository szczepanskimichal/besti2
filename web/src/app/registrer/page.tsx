import type { Metadata } from "next";
import NextLink from "next/link";
import { privatePage } from "@/lib/site";
import GoogleButton from "@/components/GoogleButton/GoogleButton";
import RegisterForm from "./RegisterForm";
import styles from "../logg-inn/page.module.css";

export const metadata: Metadata = {
    title: "Opprett konto",
    robots: privatePage,
};

type Props = {
    searchParams: Promise<{ sendt?: string; neste?: string }>;
};

export default async function RegisterCustomerPage({ searchParams }: Props) {
    const { sendt, neste } = await searchParams;
// After sign-up: tell the customer to open the mail
    if (sendt) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Sjekk e-posten din</h1>
                <p className={styles.muted}>
                    Vi har sendt deg en lenke. Klikk på den for å bekrefte e-postadressen, så kan du logge inn.
                </p>
                <p className={styles.muted}>
                    Fikk du ingen e-post? Sjekk søppelpost, eller registrer deg på nytt med samme e-post, så sender vi en ny lenke.
                </p>
            </main>
        );
    }
    return (
        <main className={styles.main}>
            <h1 className={`${styles.welcome} ${styles.centered}`}>Bli med i BESTI</h1>
            <GoogleButton next={neste} signup/>
            <RegisterForm neste={neste ?? ""} />
            <p className={`${styles.sparkleLink} ${styles.bigLink}`}>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img src="/ikoner/iskra.svg" alt="" className={styles.sparkle}/>
                Har du allerede konto? <NextLink href={`/logg-inn?neste=${encodeURIComponent(neste ?? "/")}`}>Logg inn</NextLink>
            </p>
        </main>
    );
}