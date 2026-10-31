import type { Metadata } from "next";
import NextLink from "next/link";
import { cookies } from "next/headers";
import { NEXT_COOKIE } from "@/lib/auth";
import { confirmEmail } from "@/lib/api";
import Welcome from "@/components/Welcome/Welcome";
import welcome from "@/components/Welcome/Welcome.module.css";
import styles from "../logg-inn/page.module.css";
import { privatePage } from "@/lib/site";

export const metadata: Metadata = { title: "Bekreft e-post", robots: privatePage };

type Props = {
    searchParams: Promise<{ id?: string; kode?: string }>;
};

// The link in the confirmation mail opens this page; it confirms the email right away
// and welcomes the new besti with the crystal star (only here, so it is seen once)
export default async function ConfirmEmailPage({ searchParams }: Props) {
    const { id, kode } = await searchParams;
    const confirmed = id && kode ? await confirmEmail(id, kode) : null;
    if (!confirmed) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Lenken virker ikke</h1>
                <p className={styles.muted}>
                    Lenken er ugyldig eller utløpt. Prøv å registrere deg på nytt, eller kontakt oss.
                </p>
            </main>
        );
    }

    // Signed up during a booking (same browser): back to the chosen time after logging in
    const next = (await cookies()).get(NEXT_COOKIE)?.value;
    if (confirmed.isBusiness) {
        return (
            <Welcome title="Velkommen til BESTI!" text="E-posten er bekreftet. Logg inn for å sette opp bedriften.">
                <NextLink href="/logg-inn" className={welcome.go}>Logg inn</NextLink>
            </Welcome>
        );
    }
    return (
        <Welcome big title={confirmed.firstName ? `Velkommen, ${confirmed.firstName}!` : "Velkommen!"}
                 text="Nå er du en besti">
            <NextLink href={`/logg-inn?neste=${encodeURIComponent(next ?? "/")}`} className={welcome.go}>
                {next ? "Fortsett bestillingen" : "Finn din første time"}
            </NextLink>
        </Welcome>
    );
}
