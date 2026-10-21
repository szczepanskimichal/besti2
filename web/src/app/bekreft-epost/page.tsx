import NextLink from "next/link";
import { cookies } from "next/headers";
import { NEXT_COOKIE } from "@/lib/auth";
import { confirmEmail } from "@/lib/api";
import styles from "../logg-inn/page.module.css";

type Props = {
    searchParams: Promise<{ id?: string; kode?: string }>;
};

    // The link in the confirmation mail opens this page; it confirms the email right away
export default async function ConfirmEmailPage({ searchParams }: Props) {
    const { id, kode } = await searchParams;
    const confirmed = id && kode ? await confirmEmail(id, kode) : false;
    // Signed up during a booking (same browser): "Logg inn" goes back to the chosen time
    const next = (await cookies()).get(NEXT_COOKIE)?.value;
    const loginHref = next ? `/logg-inn?neste=${encodeURIComponent(next)}` : "/logg-inn";
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>{confirmed ? "E-posten er bekreftet" : "Lenken virker ikke"}</h1>
            <p className={styles.muted}>
                {confirmed
                    ? "Takk! Nå kan du logge inn."
                    : "Lenken er ugyldig eller utløpt. Prøv å registrere deg på nytt, eller kontakt oss."}
            </p>
            {confirmed && (
                <NextLink href={loginHref} className={styles.primaryButton}>Logg inn</NextLink>
            )}
        </main>
    );
}
