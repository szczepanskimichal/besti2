import type { Metadata } from "next";
import Link from "next/link";
import { privatePage } from "@/lib/site";
import panelStyles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Kontoen er slettet",
    robots: privatePage,
};

// After "Slett konto": the customer is already logged out
export default function AccountDeletedPage() {
    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Kontoen din er slettet</h1>
            <p>Takk for at du brukte BESTI. Kommende timer er avlyst, og bedriftene har fått beskjed.</p>
            <p><Link href="/">Til forsiden</Link></p>
        </main>
    );
}