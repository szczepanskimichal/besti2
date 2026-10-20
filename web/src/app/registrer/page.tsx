import type { Metadata } from "next";
import NextLink from "next/link";
import { registerCustomerAction } from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../logg-inn/page.module.css";

export const metadata: Metadata = {
    title: "Opprett konto",
};

type Props = {
    searchParams: Promise<{ feil?: string; sendt?: string }>;
};

export default async function RegisterCustomerPage({ searchParams }: Props) {
    const { feil, sendt } = await searchParams;
// After sign-up: tell the customer to open the mail
    if (sendt) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Sjekk e-posten din</h1>
                <p className={styles.muted}>
                    Vi har sendt deg en lenke. Klikk på den for å bekrefte e-postadressen, så kan du logge inn.
                </p>
            </main>
        );
    }
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Opprett konto</h1>
            <p className={styles.muted}>Med en konto kan du bestille time og se bestillingene dine.</p>
            {feil && <p className={styles.error} role="alert">{feil}</p>}
            <form action={registerCustomerAction} className={styles.form}>
                <label className={styles.field}>
                    Fullt navn
                    <input name="fullName" required maxLength={200} autoComplete="name" />
                </label>
                <label className={styles.field}>
                    E-post
                    <input name="email" type="email" required maxLength={250} autoComplete="email" />
                </label>
                <label className={styles.field}>
                    Passord
                    <input name="password" type="password" required minLength={6} autoComplete="new-password" />
                </label>
                <p className={styles.muted}>Minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.</p>
                <p className={styles.muted}>
                    Ved å opprette konto godtar du <NextLink href="/vilkar">vilkårene</NextLink> og bekrefter at du
                    har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
                </p>
                <SubmitButton className={styles.primaryButton}>Opprett konto</SubmitButton>
            </form>
            <p className={styles.muted}>
                Har du allerede konto? <NextLink href="/logg-inn">Logg inn</NextLink>
            </p>
        </main>
    );
}
