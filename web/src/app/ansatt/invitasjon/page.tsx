import type { Metadata } from "next";
import Link from "next/link";
import { getMe, getStaffInvite } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { privatePage } from "@/lib/site";
import AcceptForm from "./AcceptForm";
import panelStyles from "../../panel/page.module.css";
import styles from "../page.module.css";

export const metadata: Metadata = {
    title: "Invitasjon for ansatte",
    robots: privatePage,
};

type InvitePageProps = {
    searchParams: Promise<{ kode?: string }>;
};

// The link in the e-mail "Du er invitert til ... på BESTI": log in (or create an account) with that e-mail, then accept
export default async function StaffInvitePage({ searchParams }: InvitePageProps) {
    const { kode } = await searchParams;
    const code = kode && /^[A-Za-z0-9_-]{10,100}$/.test(kode) ? kode : null;
    const invite = code ? await getStaffInvite(code).catch(() => null) : null;
    const token = await getToken();
    const me = token ? await getMe(token).catch(() => null) : null;
    const here = `/ansatt/invitasjon?kode=${code ?? ""}`;

    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Invitasjon</h1>
            {!code || !invite ? (
                <p className={panelStyles.muted}>
                    Lenken er ugyldig eller utløpt. Be bedriften sende en ny invitasjon.
                </p>
            ) : (
                <section className={`${panelStyles.card} ${styles.invite}`}>
                    <p>
                        <strong>{invite.businessName}</strong> vil at du skal se dine egne timer som{" "}
                        <strong>{invite.employeeName}</strong> på BESTI.
                    </p>
                    <p className={panelStyles.muted}>
                        Du ser bare din egen arbeidsplan og kundene dine. Priser, statistikk og innstillinger ser du
                        ikke. Invitasjonen gjelder {invite.email}.
                    </p>
                    {!me ? (
                        <p>
                            <Link href={`/logg-inn?neste=${encodeURIComponent(here)}`}
                                  className={panelStyles.primaryButton}>Logg inn</Link>{" "}
                            <Link href={`/registrer?neste=${encodeURIComponent(here)}`}>Opprett konto</Link>
                        </p>
                    ) : me.isBusiness ? (
                        <p className={panelStyles.error} role="alert">
                            Du er logget inn med en bedriftskonto. Logg ut og logg inn med {invite.email}.
                        </p>
                    ) : (
                        <AcceptForm code={code}/>
                    )}
                </section>
            )}
        </main>
    );
}
