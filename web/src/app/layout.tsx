import  type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "besti2 – Finn og bestill time",
  description: "Finn bedrifter og bestill time på nett."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="nb">
      <body>{children}</body>
    </html>
  );
}
