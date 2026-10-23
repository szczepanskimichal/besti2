import type { Metadata } from "next";
import Link from "next/link";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Vilkår for bruk",
};

export default function TermsPage() {
    return (
        <main className={styles.main}>
            <h1>Vilkår for bruk</h1>
            <p className={styles.updated}>Sist oppdatert: 22. oktober 2026</p>
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
                    og BESTI er databehandler. Se <a href="#databehandleravtale">databehandleravtalen</a> under.
                </li>
                <li>Nye bedrifter blir sjekket og godkjent av BESTI før kundene ser dem.</li>
                <li>BESTI kan fjerne innhold som er ulovlig eller bryter vilkårene, og stenge kontoer ved misbruk.</li>
            </ul>
            <h2 id="databehandleravtale">Databehandleravtale</h2>
            <p>
                Denne delen er databehandleravtalen mellom bedriften (behandlingsansvarlig) og BESTI
                (databehandler), jf. personvernforordningen artikkel 28. Bedriften godtar den når den
                registrerer seg, og den gjelder så lenge bedriften har konto hos BESTI.
            </p>
            <ul>
                <li><strong>Hva behandles:</strong> opplysninger om bedriftens kunder i forbindelse med bestillinger
                    (navn, telefon, e-post, tjeneste, ansatt, tidspunkt, status og kundens melding) og navn på
                    bedriftens ansatte.</li>
                <li><strong>Formål:</strong> å ta imot, vise, flytte og avlyse bestillinger og sende bekreftelser og
                    varsler på vegne av bedriften. BESTI bruker ikke opplysningene til egne formål, som markedsføring.</li>
                <li><strong>Instrukser:</strong> BESTI behandler opplysningene bare etter bedriftens instrukser, slik de
                    følger av denne avtalen og av det bedriften gjør i tjenesten.</li>
                <li><strong>Taushetsplikt og sikkerhet:</strong> bare personer som trenger det for å drifte tjenesten,
                    har tilgang, og de har taushetsplikt. Opplysningene beskyttes med kryptert forbindelse (HTTPS),
                    tilgangskontroll og sikkerhetskopier (artikkel 32).</li>
                <li><strong>Underleverandører:</strong> bedriften godtar underleverandørene som står i{" "}
                    <Link href="/personvern#underleverandorer">personvernerklæringen</Link>. BESTI har
                    databehandleravtaler med dem og varsler bedriften før en ny underleverandør tas i bruk, slik at
                    bedriften kan protestere.</li>
                <li><strong>Hjelp til bedriften:</strong> BESTI hjelper bedriften med å svare på kunders krav om innsyn,
                    retting og sletting, og med sikkerhet og konsekvensvurderinger når det trengs.</li>
                <li><strong>Avvik:</strong> BESTI varsler bedriften uten ugrunnet opphold, og senest innen 48 timer,
                    hvis et brudd på personopplysningssikkerheten gjelder bedriftens kunder.</li>
                <li><strong>Kontroll:</strong> BESTI gir bedriften informasjonen som trengs for å vise at avtalen
                    følges.</li>
                <li><strong>Når avtalen slutter:</strong> når bedriften avslutter kontoen, sletter eller anonymiserer
                    BESTI kundeopplysningene, med mindre loven krever at de lagres lenger. Bedriften kan først be om en
                    kopi.</li>
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
                Kontakt oss på post@bestiapp.no. Norsk lov gjelder.
            </p>
        </main>
    );
}
