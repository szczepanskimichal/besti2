import type { Metadata, Viewport } from "next";
import { Nunito } from "next/font/google";
import "./globals.css";
import Header from "../components/Header/Header";
import Footer from "../components/Footer/Footer";
import { ALLOW_INDEXING, SITE_URL } from "@/lib/site";
import Aurora from "../components/Aurora/Aurora";
import Analytics from "../components/Analytics/Analytics";

const nunito = Nunito({
    subsets: ["latin", "latin-ext"],
    weight: ["400", "600", "700", "800"],
    variable: "--font-nunito",
    display: "swap",
});

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

// Colour of the phone's status bar, same as the bottom of the aurora sky
export const viewport: Viewport = {
    themeColor: "#04141c",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="nb" className={nunito.variable}>
        <body>
        <Aurora />
        <Header />
        {children}
        <Footer/>
        <Analytics/>
        </body>
        </html>
    );
}