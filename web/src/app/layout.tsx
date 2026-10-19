import  type { Metadata } from "next";
import "./globals.css";
import Header from "../components/Header/Header";
import Footer from "../components/Footer/Footer";
export const metadata: Metadata = {
    title: {
        default: "BESTI – Finn og bestill time",
        template: "%s | BESTI",
    },
    description: "Finn frisører, håndverkere og andre lokale bedrifter i Norge, se ledige tider og bestill time på nett.",
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
