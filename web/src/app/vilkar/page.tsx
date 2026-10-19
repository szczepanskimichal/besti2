import type { Metadata } from "next";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Vilkår for bruk",
};

export default function TermsPage() {
    return (
        <main className={styles.main}>
            <h1>Vilkår for bruk</h1>
            <p className={styles.updated}>Sist oppdatert: 19. oktober 2026</p>
            <h2>Om tjenesten</h2>
            <p>
                BESTI er en tjeneste for å finne bedrifter og bestille time på nett. Avtalen om selve
                tjenesten (for eksempel en hårklipp eller en befaring) inngås mellom deg og bedriften.
                BESTI er ikke part i den avtalen.
            </p>
            <h2>For kunder</h2>
            <ul>
                <li>Oppgi riktig navn, telefon og e-post når du bestiller.</li>
                <li>Bedriften kan bekrefte eller avlyse bestillingen. Du får beskjed på e-post.</li>
                <li>Ønsker du å endre eller avbestille, kontakt bedriften så tidlig som mulig.</li>
                <li>Pris og betaling avtales med bedriften. BESTI tar ikke imot betaling.</li>
            </ul>
            <h2>For bedrifter</h2>
            <ul>
                <li>Opplysningene om bedriften (navn, organisasjonsnummer, adresse, priser) skal være riktige og oppdaterte.</li>
                <li>Du er ansvarlig for tjenestene du tilbyr og for å følge lover og regler for din bransje.</li>
                <li>Du kan bare laste opp bilder du har rettigheter til. Personer på bildene må ha gitt samtykke.</li>
                <li>Kundeopplysninger du får gjennom BESTI kan bare brukes til å gi kunden timen.</li>
                <li>
                    Når kunder bestiller time hos deg, er du behandlingsansvarlig for kundenes opplysninger,
                    og BESTI er databehandler. BESTI behandler opplysningene bare for å levere tjenesten,
                    holder dem sikre og deler dem bare med nødvendige underleverandører. Disse vilkårene
                    er databehandleravtalen mellom deg og BESTI.
                </li>
                <li>BESTI kan fjerne innhold som er ulovlig eller bryter vilkårene, og stenge kontoer ved misbruk.</li>
            </ul>
            <h2>Ansvar</h2>
            <p>
                Tjenesten er gratis i testperioden. Vi varsler før eventuelle priser innføres.
            </p>
            <p>
                Vi gjør vårt beste for at tjenesten fungerer, men kan ikke garantere at den alltid er
                tilgjengelig. BESTI er ikke ansvarlig for tjenester som utføres av bedriftene.
            </p>
            <h2>Endringer og kontakt</h2>
            <p>
                Vi kan oppdatere vilkårene. Vesentlige endringer varsles på nettsiden. Spørsmål?
                Kontakt oss på michalszczepanski07@gmail.com. Norsk lov gjelder.
            </p>
        </main>
    );
}
