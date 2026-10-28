import type { Metadata } from "next";
import Link from "next/link";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Personvernerklæring",
};

// Must always describe what the app really does. When personal data, purposes, suppliers or storage periods
// change: update this page and docs/personvern-oversikt.md in the same pull request
export default function PrivacyPage() {
    return (
        <main className={styles.main}>
            <h1>Personvernerklæring</h1>
            <p className={styles.updated}>Sist oppdatert: 28. oktober 2026</p>

            <p>
                Her forklarer vi hvilke personopplysninger BESTI behandler, hvorfor, hvor lenge og hvilke rettigheter
                du har. Behandlingen følger personopplysningsloven og personvernforordningen (GDPR).
            </p>

            <h2>Hvem er ansvarlig?</h2>
            <p>
                BESTI drives av Michał Szczepański. BESTI er behandlingsansvarlig for brukerkontoer (kunder og bedrifter),
                vurderinger og varsler i tjenesten. For opplysningene om en bestilling er bedriften du bestiller hos
                også ansvarlig: bedriften bruker opplysningene for å gi deg timen, og BESTI behandler dem på vegne av
                bedriften. Kontakt: <a href="mailto:personvern@bestiapp.no">personvern@bestiapp.no</a>, BESTI,
                Gjøvik.
            </p>

            <h2>Hvilke opplysninger behandler vi?</h2>
            <ul>
                <li><strong>Kundekonto:</strong> navn, e-post, passord (lagres bare kryptert som hash) og, hvis du vil,
                    telefonnummer. Logger du inn med Google, får vi navnet, e-postadressen og en ID for
                    Google-kontoen din fra Google, men aldri passordet ditt hos Google.</li>
                <li><strong>Bestillinger:</strong> navn, telefon, e-post, valgt bedrift, tjeneste, ansatt og tidspunkt,
                    status (venter, bekreftet, avlyst, ny tid, møtte ikke), en melding til bedriften hvis du skriver
                    en, og en grunn når du avbestiller, hvis du skriver en (den sendes til bedriften og lagres ikke på
                    timen). Ikke skriv sensitive opplysninger, for
                    eksempel om helse, i meldingsfeltene.</li>
                <li><strong>«Møtte ikke»:</strong> bedriften kan registrere at du ikke kom til timen. Bare den
                    bedriften ser hvor mange ganger du ikke har møtt hos dem. Andre bedrifter ser det aldri, og
                    BESTI bruker det ikke til noe annet.</li>
                <li><strong>Vurderinger:</strong> stjerner, kommentar og fornavnet ditt, som vises på bedriftens side.</li>
                <li><strong>Varsler i tjenesten</strong> (bjellen): korte meldinger om dine timer.</li>
                <li><strong>Bedrifter:</strong> bedriftsnavn, organisasjonsnummer, adresse, telefon, e-post til
                    innlogging (vises aldri) og eventuelt til kunder, nettside, beskrivelse, ansatte (navn),
                    åpningstider, logo, bilder, blokkert tid og eventuelt hvor dere hørte om BESTI.</li>
                <li><strong>Teknisk:</strong> IP-adresse brukes kortvarig for å stoppe misbruk (for mange forsøk på
                    innlogging eller bestilling) og kan stå i serverlogger.</li>
                <li><strong>Besøksstatistikk:</strong> hvilke offentlige sider som besøkes, hvilken nettside du kom
                    fra (for eksempel Instagram eller en QR-kode), land, nettleser, operativsystem og skjermstørrelse.
                    Uten informasjonskapsler, og uten at vi kan se hvem du er. Sider der du er innlogget (Min Besti,
                    bedriftspanelet, varsler) telles ikke.</li>
            </ul>

            <h2>Hvorfor, og med hvilket grunnlag?</h2>
            <ul>
                <li>For å opprette kontoen din, gjennomføre bestillinger og sende bekreftelser, endringer og avlysninger
                    på e-post og i varsler, og en påminnelse på e-post dagen før timen (avtale, GDPR art. 6 nr. 1 b).
                    Legger bedriften inn timen for deg, får du e-post bare hvis bedriften velger å sende
                    bekreftelse.</li>
                <li>Bedriften ser navn, telefon, e-post og meldingen din, slik at de kan gi deg timen, bekrefte, flytte
                    eller avlyse den.</li>
                <li>Vurderinger hjelper andre kunder å velge, og bedriftene å bli bedre (berettiget interesse, art. 6 nr. 1 f).</li>
                <li>«Møtte ikke» hjelper bedriften å planlegge når en kunde ofte ikke kommer (berettiget interesse,
                    art. 6 nr. 1 f). Du får beskjed på e-post når det blir registrert, og kan ikke gi vurdering av den
                    timen. Var du der likevel, kontakt bedriften eller skriv til{" "}
                    <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>, så ser vi på det.</li>
                <li>Sikkerhet og å hindre misbruk (berettiget interesse, art. 6 nr. 1 f).</li>
                <li>Besøksstatistikk, for å se hvilke kanaler som gir nye kunder og bedrifter og gjøre tjenesten
                    bedre (berettiget interesse, art. 6 nr. 1 f).</li>
                <li>Når en bedrift registrerer seg, sjekker vi organisasjonsnummeret i Enhetsregisteret
                    (Brønnøysundregistrene) og godkjenner bedriften før kundene ser den, for å hindre falske
                    bedrifter (berettiget interesse, art. 6 nr. 1 f).</li>
            </ul>

            <h2>Hvem deler vi med?</h2>
            <p>Vi selger aldri personopplysninger og bruker ikke annonsenettverk. Opplysninger deles bare slik:</p>
            <ul>
                <li><strong>Bedriften du bestiller hos</strong> ser bestillingen din.</li>
                <li><strong>Leverandører</strong> som drifter tjenesten for oss. De behandler opplysningene bare etter
                    våre instrukser (databehandleravtale):
                    De tre første er også databehandlere for bedriftene (se databehandleravtalen i{" "}
                    <Link href="/vilkar#databehandleravtale">vilkårene</Link>).
                    <ul id="underleverandorer">
                        <li>Hetzner Online GmbH (Tyskland): servere og database i datasenter i Finland, og
                            sikkerhetskopier i datasenter i Tyskland. Databehandler for bedriftene.</li>
                        <li>Brevo (Sendinblue SAS, Frankrike): utsending av e-post. Databehandler for bedriftene.</li>
                        <li>Cloudflare, Inc. (USA): databehandler for bedriftene. domenenavn, beskyttelse mot angrep og sikker videreformidling av
                            trafikken til nettsiden. Trafikken, også IP-adressen din, går gjennom Cloudflare.
                            Overføringen til USA skjer under EU–US Data Privacy Framework og EUs
                            standardkontraktsbestemmelser.</li>
                        <li>Umami Software, Inc. (USA): besøksstatistikk uten informasjonskapsler. IP-adressen brukes
                            bare til å finne land og lagres ikke. Statistikken lagres som tall som ikke kan knyttes
                            til deg. Får aldri opplysninger om bestillinger.</li>
                    </ul>
                </li>
                <li><strong>Google</strong> (Google Ireland Ltd.): bare hvis du velger «Fortsett med Google». Da
                    logger du inn hos Google, og Google sender oss navnet og e-postadressen din. Google ser at du
                    logger inn på BESTI, etter Googles egne regler for personvern.</li>
                <li><strong>Bedriftens nettside:</strong> når du klikker på lenken, forlater du BESTI. Vi sender ingen
                    opplysninger om deg dit, og nettsiden har sine egne regler for personvern.</li>
                <li><strong>Kartverket</strong> får bedriftens adresse for å finne koordinater til kartet, og
                    <strong> Brønnøysundregistrene</strong> får bedriftens organisasjonsnummer. Ingen opplysninger om
                    kunder sendes dit.</li>
            </ul>

            <h2>Hvor lenge lagrer vi opplysningene?</h2>
            <ul>
                <li><strong>Bestillinger:</strong> i 3 år etter timen, slik at bedriften har historikk og timen kan
                    dokumenteres ved en eventuell uenighet (den vanlige foreldelsesfristen er 3 år). Deretter
                    anonymiseres de automatisk (navn, telefon, e-post og melding fjernes).</li>
                <li><strong>Vurderinger:</strong> 3 år etter timen fjernes navnet ditt, og vurderingen vises som
                    «Tidligere kunde». Stjernene og teksten blir stående, slik at bedriftens snitt forblir riktig.</li>
                <li><strong>Varsler:</strong> til du sletter dem, men aldri lenger enn 7 dager for kunder og 14 dager
                    for bedrifter.</li>
                <li><strong>Kundekonto:</strong> til du sletter den. Når du sletter kontoen, blir kommende timer
                    avlyst (bedriften får beskjed), tidligere timer anonymiseres med en gang, varslene slettes, og
                    vurderingene dine blir stående som «Tidligere kunde» uten navnet ditt.</li>
                <li><strong>Bedriftskonto:</strong> til bedriften sletter den i panelet (Bedriftsprofil → «Slett
                    bedrift»). Da slettes bedriften, de ansatte, bildene, alle timer og vurderinger hos bedriften og
                    innloggingen med en gang. Kommende timer må være avlyst først, slik at kundene får beskjed.</li>
                <li><strong>Bilder</strong> blir forminsket og lagret uten skjulte bildedata (EXIF), som GPS-posisjon.</li>
                <li><strong>Sikkerhetskopier</strong> av databasen og bildene tas hver natt og slettes etter
                    14 dager. Det du sletter, er derfor helt borte også fra sikkerhetskopiene innen 14 dager.</li>
            </ul>

            <h2>Må du gi oss opplysningene?</h2>
            <p>
                For å bestille må du ha en konto med navn og e-post, og bedriften trenger telefonnummeret ditt for
                å kunne nå deg. Uten dem kan du ikke bestille på nett, men du kan alltid ringe bedriften. Alt annet,
                som melding til bedriften og vurderinger, er frivillig.
            </p>
            <p>
                Vi tar ingen automatiserte avgjørelser om deg og lager ingen profiler av deg. Rekkefølgen i søket
                (for eksempel «Best vurdert») gjelder bedriftene, ikke kundene. Tjenesten er ikke laget for barn
                under 13 år.
            </p>

            <h2>Informasjonskapsler (cookies)</h2>
            <p>
                Vi bruker bare nødvendige informasjonskapsler: én som holder deg innlogget, og i bedriftspanelet én
                som husker at listen «Kom i gang» er skjult. Besøksstatistikken bruker ingen informasjonskapsler og
                lagrer ingenting i nettleseren din. Vi bruker ikke reklame-cookies eller sporing på tvers av
                nettsider, og trenger derfor ikke samtykke til cookies.
            </p>

            <h2>Dine rettigheter</h2>
            <ul>
                <li><strong>Innsyn og dataportabilitet:</strong> last ned alt vi lagrer om deg under
                    {" "}<Link href="/min-besti/profil">Min Besti → Profil → «Last ned mine data»</Link>.</li>
                <li><strong>Retting:</strong> endre navn og telefon under Profil.</li>
                <li><strong>Sletting:</strong> slett kontoen under Profil → «Slett konto». En bedrift sletter sin
                    konto i panelet under Bedriftsprofil → «Slett bedrift».</li>
                <li><strong>Protest og begrensning:</strong> protester mot behandling vi gjør på grunnlag av
                    berettiget interesse, eller be oss begrense behandlingen, ved å kontakte oss.</li>
                <li>Gjelder det en bestilling, kan du også kontakte bedriften direkte.</li>
                <li>Du kan klage til Datatilsynet (<a href="https://www.datatilsynet.no">datatilsynet.no</a>).</li>
            </ul>
            <p>Vi svarer på henvendelser om personvern innen 30 dager.</p>
        </main>
    );
}
