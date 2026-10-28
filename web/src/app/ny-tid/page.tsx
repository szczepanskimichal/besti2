import type { Metadata } from "next";
import Link from "next/link";
import { getNewTime } from "@/lib/api";
import { formatDate, formatTime } from "@/lib/format";
import { privatePage } from "@/lib/site";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { answerNewTimeAction } from "./actions";
// Same cards and buttons as Min Besti
import styles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Ny tid",
    robots: privatePage,
};

type NewTimePageProps = {
    searchParams: Promise<{ token?: string; svar?: string }>;
};

// "Passer den nye tiden?": the page behind the link in the e-mail. The link only shows the question;
// the answer is a button, so a mail program that opens links on its own cannot answer for the customer
export default async function NewTimePage({ searchParams }: NewTimePageProps) {
    const { token, svar } = await searchParams;

    if (svar) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Ny tid</h1>
                <p role="status">
                    {svar === "godtatt" && "Takk! Vi har gitt beskjed til bedriften, og du ser timen i Min Besti."}
                    {svar === "avslatt" && "Timen er avlyst, og bedriften har fått beskjed. Du kan bestille en ny time når det passer."}
                    {svar === "feil" && "Svaret kunne ikke lagres. Lenken kan være brukt allerede, eller timen har startet."}
                </p>
                <p><Link href="/">Til forsiden</Link></p>
            </main>
        );
    }

    const newTime = token ? await getNewTime(token) : null;
    if (!token || !newTime) {
        return (
            <main className={styles.main}>
                <h1 className={styles.title}>Ny tid</h1>
                <p>Lenken er ikke gyldig lenger. Kanskje du allerede har svart, eller bedriften har endret timen igjen.</p>
                <p><Link href="/min-besti">Se timene dine i Min Besti</Link></p>
            </main>
        );
    }

    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Passer den nye tiden?</h1>
            <div className={styles.card}>
                <p className={styles.when}>
                    {formatDate(newTime.startUtc)} kl. {formatTime(newTime.startUtc)}
                </p>
                {newTime.previousStartUtc && (
                    <p className={styles.muted}>
                        Før: {formatDate(newTime.previousStartUtc)} kl. {formatTime(newTime.previousStartUtc)}
                    </p>
                )}
                <p className={styles.service}>
                    {newTime.serviceName} hos {newTime.employeeName}, {newTime.businessName}
                </p>
                <div className={styles.actions}>
                    <form action={answerNewTimeAction}>
                        <input type="hidden" name="token" value={token}/>
                        <input type="hidden" name="svar" value="ja"/>
                        <SubmitButton className={styles.primaryButton} pendingText="Lagrer…">Godta ny tid</SubmitButton>
                    </form>
                    <form action={answerNewTimeAction}>
                        <input type="hidden" name="token" value={token}/>
                        <input type="hidden" name="svar" value="nei"/>
                        <SubmitButton className={styles.dangerButton} pendingText="Lagrer…">Passer ikke – avlys timen</SubmitButton>
                    </form>
                </div>
            </div>
        </main>
    );
}
