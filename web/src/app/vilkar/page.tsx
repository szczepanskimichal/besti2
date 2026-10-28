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
            <p className={styles.updated}>Sist oppdatert: 28. oktober 2026</p>
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
                <li>
                    <strong>Avbestilling:</strong> du kan avbestille gratis i Min Besti fram til fristen bedriften har
                    satt (for eksempel 24 timer før timen). Fristen står når du bestiller og i Min Besti. Etter
                    fristen må du ringe bedriften. BESTI tar ikke gebyr for avbestilling.
                </li>
                <li>Ønsker du å endre tiden, kontakt bedriften så tidlig som mulig.</li>
                <li>
                    <strong>«Møtte ikke»:</strong> kommer du ikke til timen uten å avbestille, kan bedriften registrere
                    det. Du får beskjed på e-post, og du kan ikke gi vurdering av den timen. Bare den bedriften ser
                    det. Var du der likevel, kontakt bedriften eller skriv til{" "}
                    <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>, så ser vi på det.
                </li>
                <li>Pris og betaling avtales med bedriften. BESTI tar ikke imot betaling.</li>
            </ul>
            <h2 id="vurderinger">Vurderinger</h2>
            <ul>
                <li>Du kan gi én vurdering for hver time du har hatt, og bare etter at timen er over.</li>
                <li>Skriv om din egen opplevelse, ærlig og saklig. Ikke skriv personangrep, trusler, hatefulle eller
                    ulovlige ytringer, reklame eller opplysninger om andre personer, for eksempel ansattes helse.</li>
                <li>Vurderingen vises med fornavnet ditt. 5 år etter timen, eller når du sletter kontoen, vises den som
                    «Tidligere kunde».</li>
                <li>Bedriften kan svare offentlig, men kan ikke slette, endre eller velge bort vurderinger. Snittet
                    regnes av alle vurderinger.</li>
                <li>BESTI kan fjerne vurderinger som bryter disse reglene. Mener du at en vurdering bryter reglene,
                    skriv til <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>.</li>
            </ul>
            <h2>For bedrifter</h2>
            <ul>
                <li>Opplysningene om bedriften (navn, organisasjonsnummer, adresse, priser) skal være riktige og oppdaterte.</li>
                <li>Du er ansvarlig for tjenestene du tilbyr og for å følge lover og regler for din bransje.</li>
                <li>Du kan bare laste opp bilder du har rettigheter til. Personer på bildene må ha gitt samtykke.</li>
                <li>Kundeopplysninger du får gjennom BESTI kan bare brukes til å gi kunden timen.</li>
                <li>Du setter selv fristen for gratis avbestilling, og den gjelder for alle timer bestilt via BESTI.</li>
                <li>Registrer «Møtte ikke» bare når kunden faktisk ikke kom. Feil registrering skal rettes med en gang
                    («Møtte»).</li>
                <li>Legger du inn en time for en kunde selv, sender du bare bekreftelse og påminnelse på e-post når
                    kunden har bedt om det.</li>
                <li>
                    Når kunder bestiller time hos deg, er du behandlingsansvarlig for kundenes opplysninger,
                    og BESTI er databehandler. Se <a href="#databehandleravtale">databehandleravtalen</a> under.
                </li>
                <li>Nye bedrifter blir sjekket og godkjent av BESTI før kundene ser dem.</li>
                <li>BESTI kan fjerne innhold som er ulovlig eller bryter vilkårene, og stenge kontoer ved misbruk.</li>
                <li>Du kan slette bedriften når som helst i panelet (Bedriftsprofil → «Slett bedrift»). Kommende timer
                    må avlyses først, slik at kundene får beskjed.</li>
            </ul>
            <h2 id="databehandleravtale">Databehandleravtale</h2>
            <p>
                Denne delen er databehandleravtalen mellom bedriften (behandlingsansvarlig) og BESTI
                (databehandler), jf. personvernforordningen artikkel 28. Bedriften godtar den når den
                registrerer seg, og den gjelder så lenge bedriften har konto hos BESTI.
            </p>
            <ul>
                <li><strong>Hva behandles:</strong> opplysninger om bedriftens kunder i forbindelse med bestillinger
                    (navn, telefon, e-post, tjeneste, ansatt, tidspunkt, status inkludert «møtte ikke», kundens
                    melding og grunn for avbestilling) og navn på bedriftens ansatte.</li>
                <li><strong>Hvor lenge:</strong> bestillinger anonymiseres automatisk 5 år etter timen. Sikkerhetskopier
                    slettes etter 14 dager.</li>
                <li><strong>Formål:</strong> å ta imot, vise, flytte og avlyse bestillinger og sende bekreftelser,
                    påminnelser og varsler på vegne av bedriften. BESTI bruker ikke opplysningene til egne formål, som markedsføring.</li>
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
                <li><strong>Når avtalen slutter:</strong> når bedriften sletter kontoen («Slett bedrift»), sletter
                    BESTI kundeopplysningene med en gang, og de er borte fra sikkerhetskopiene innen 14 dager. Bedriften
                    kan først be om en kopi på <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>.</li>
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
