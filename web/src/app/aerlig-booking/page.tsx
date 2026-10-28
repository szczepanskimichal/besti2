import type { Metadata } from "next";
import Link from "next/link";
import VerifiedBadge from "@/components/VerifiedBadge/VerifiedBadge";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Ærlig booking",
    description: "Ekte bedrifter, ekte vurderinger og et søk som ikke kan kjøpes. Slik fungerer BESTI.",
    alternates: { canonical: "/aerlig-booking" },
};

// Every promise here must be true in the code. When a rule changes (reviews, ranking, ads, storage):
// update this page in the same pull request
export default function HonestBookingPage() {
    return (
        <main className={styles.main}>
            <h1>Ærlig booking</h1>
            <p className={styles.updated}>Slik fungerer BESTI</p>

            <p>
                BESTI er laget for at du skal kunne stole på det du ser: bedriftene er ekte, vurderingene er ekte,
                og rekkefølgen i søket kan ikke kjøpes.
            </p>

            <h2 id="verifisert">Verifisert bedrift</h2>
            <p><VerifiedBadge/></p>
            <p>Merket betyr at:</p>
            <ul>
                <li>organisasjonsnummeret er aktivt i Enhetsregisteret (Brønnøysundregistrene), altså ikke slettet,
                    konkurs eller under avvikling,</li>
                <li>BESTI har godkjent bedriften før den ble synlig for kundene,</li>
                <li>vi sjekker Brønnøysundregistrene på nytt hver uke. Blir bedriften slettet eller konkurs,
                    forsvinner merket av seg selv.</li>
            </ul>
            <p>
                Merket viser at bedriften finnes og er registrert. Det sier ikke noe om kvaliteten på arbeidet.
                Til det har vi vurderingene.
            </p>

            <h2>Bare ekte vurderinger</h2>
            <ul>
                <li>Bare kunder som har hatt time hos bedriften gjennom BESTI, kan gi vurdering, én per time, og
                    først når timen er over.</li>
                <li>Bedriften kan svare på en vurdering, men ikke endre eller slette den.</li>
                <li>Snittet vises først fra 3 vurderinger, så én dårlig (eller én altfor god) vurdering ikke
                    avgjør alt. Før det står det «Ny på Besti».</li>
                <li>«Best vurdert» bruker et vektet snitt: mange gode vurderinger teller mer enn én enkelt femmer.</li>
            </ul>

            <h2>Søket kan ikke kjøpes</h2>
            <ul>
                <li>Rekkefølgen bestemmes av det du velger: navn, avstand, ledige tider eller vurderinger. Ingen
                    bedrift kan betale for å komme høyere opp.</li>
                <li>Hvis vi en dag viser betalte plasseringer, er de alltid tydelig merket «Annonse», og de vanlige
                    resultatene er de samme som før.</li>
            </ul>

            <h2>Pris og regler før du bestiller</h2>
            <ul>
                <li>Pris og varighet står ved hver tjeneste. «Pris etter avtale» betyr at prisen avtales med
                    bedriften, for eksempel etter en befaring.</li>
                <li>Fristen for gratis avbestilling står på siden til bedriften før du bestiller.</li>
                <li>Du får bekreftelse på e-post og en påminnelse dagen før timen.</li>
            </ul>

            <h2>Ingen sporing</h2>
            <ul>
                <li>Ingen informasjonskapsler for sporing eller reklame. Besøksstatistikken vår er uten
                    informasjonskapsler og viser ikke hvem du er.</li>
                <li>Vi selger aldri personopplysninger og bruker ikke annonsenettverk.</li>
                <li>Gamle bestillinger blir anonymisert etter 3 år. Les mer i{" "}
                    <Link href="/personvern">personvernerklæringen</Link>.</li>
            </ul>

            <h2>For bedrifter</h2>
            <ul>
                <li>Kunder som bestiller via din egen lenke eller QR-kode, er dine kunder. BESTI tar aldri
                    provisjon i prosent av det kundene betaler deg.</li>
                <li>Du får forslag til tjenester for bransjen din, så siden er klar på et par minutter.</li>
                <li>Alle bedrifter sjekkes i Brønnøysundregistrene, så kundene vet at de bestiller hos en ekte
                    bedrift.</li>
            </ul>
            <p><Link href="/registrer-bedrift">Registrer bedriften din</Link></p>
        </main>
    );
}
