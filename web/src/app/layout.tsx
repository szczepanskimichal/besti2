import  type { Metadata } from "next";
import "./globals.css";
import Header from "../components/Header/Header";
import Footer from "../components/Footer/Footer";
import { SITE_URL } from "@/lib/site";
export const metadata: Metadata = {
    // Full addresses for links shared on Facebook, Messenger etc.
    metadataBase: new URL(SITE_URL),
    title: {
        default: "BESTI – Finn og bestill time",
        template: "%s | BESTI",
    },
    description: "Finn frisører, håndverkere og andre lokale bedrifter i Norge, se ledige tider og bestill time på nett.",
    openGraph: { siteName: "BESTI", locale: "nb_NO", type: "website" },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="nb">
        <body>
        <Header />
        {children}
        <Footer/>
        </body>
        </html>
    );
}
