import type { Metadata } from "next";
import Link from "next/link";
import { privatePage } from "@/lib/site";
import panelStyles from "../panel/page.module.css";

export const metadata: Metadata = {
    title: "Kontoen er slettet",
    robots: privatePage,
};

type AccountDeletedPageProps = {
    searchParams: Promise<{ bedrift?: string }>;
};

// After "Slett konto" (customer) or "Slett bedrift" (?bedrift=1): already logged out
export default async function AccountDeletedPage({ searchParams }: AccountDeletedPageProps) {
    const { bedrift } = await searchParams;

    return (
        <main className={panelStyles.main}>
            {bedrift ? (
                <>
                    <h1 className={panelStyles.title}>Bedriften er slettet</h1>
                    <p>Takk for at dere brukte BESTI. Bedriften og alle opplysningene om den er slettet.</p>
                </>
            ) : (
                <>
                    <h1 className={panelStyles.title}>Kontoen din er slettet</h1>
                    <p>Takk for at du brukte BESTI. Kommende timer er avlyst, og bedriftene har fått beskjed.</p>
                </>
            )}
            <p><Link href="/">Til forsiden</Link></p>
        </main>
    );
}
