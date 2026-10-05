import  type { Metadata } from "next";
import "./globals.css";
import Header from "../components/Header/Header";

export const metadata: Metadata = {
  title: "besti2 – Finn og bestill time",
  description: "Finn bedrifter og bestill time på nett."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="nb">
      <body>
      <Header />
      {children}
      </body>
    </html>
  );
}
