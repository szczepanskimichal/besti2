import  type { Metadata } from "next";
import "./globals.css";
import Header from "../components/Header/Header";
import Footer from "../components/Footer/Footer";
import { ALLOW_INDEXING, SITE_URL } from "@/lib/site";
export const metadata: Metadata = {
    // Full addresses for links shared on Facebook, Messenger etc.
    metadataBase: new URL(SITE_URL),
    title: {
        default: "BESTI – Finn og bestill time",
        template: "%s | BESTI",
    },
    description: "Finn frisører, håndverkere og andre lokale bedrifter i Norge, se ledige tider og bestill time på nett.",
    openGraph: { siteName: "BESTI", locale: "nb_NO", type: "website" },
    // Not in Google yet (see ALLOW_INDEXING); pages with their own robots setting keep it
    robots: ALLOW_INDEXING ? undefined : { index: false, follow: false },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="nb" className={nunito.variable}>
        <body>
        <Header />
        {children}
        <Footer/>
        </body>
        </html>
    );
}