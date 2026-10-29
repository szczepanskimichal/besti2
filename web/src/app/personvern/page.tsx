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
            <p className={styles.updated}>Sist oppdatert: 29. oktober 2026</p>

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
                    Google-kontoen din fra Google, men aldri passordet ditt hos Google. Vi lagrer også valgene dine
                    under Varsler (e-post dagen før timen og «Snart tid!» to timer før).</li>
                <li><strong>Bestillinger:</strong> navn, telefon, e-post, valgt bedrift, tjeneste, ansatt og tidspunkt,
                    status (venter, bekreftet, avlyst, ny tid, møtte ikke), en melding til bedriften hvis du skriver
                    en, og en grunn når du avbestiller, hvis du skriver en (den sendes til bedriften og lagres ikke på
                    timen). Ikke skriv sensitive opplysninger, for
                    eksempel om helse, i meldingsfeltene.</li>
                <li><strong>«Møtte ikke»:</strong> bedriften kan registrere at du ikke kom til timen. Bare den
                    bedriften ser hvor mange ganger du ikke har møtt hos dem. Andre bedrifter ser det aldri, og
                    BESTI bruker det ikke til noe annet.</li>
                <li><strong>Vurderinger:</strong> stjerner, kommentar, fornavnet ditt og eventuelt et bilde fra
                    besøket, som vises på bedriftens side. Ikke last opp bilder der andre personer kan kjennes igjen
                    uten at de har sagt ja. Andre kunder kan markere en vurdering som «Nyttig»: vi lagrer hvem som
                    markerte, men viser bare antallet.</li>
                <li><strong>Varsler i tjenesten</strong> (bjellen): korte meldinger om dine timer.</li>
                <li><strong>Varsler på telefonen</strong> (bare hvis du slår det på): en adresse fra nettleserens
                    push-tjeneste og to nøkler, slik at vi kan sende bjellens meldinger til enheten din.</li>
                <li><strong>Favoritter, venteliste og «Tid for ny time?»:</strong> bedrifter du har lagt til som
                    favoritt, dager du står på venteliste for («Si fra hvis det blir ledig») og påminnelser du selv har
                    bedt om. Dette ser bare du.</li>
                <li><strong>BESTI sammen:</strong> når du inviterer venner, lagrer vi invitasjonen (bedrift, tjeneste,
                    tid, antall personer, telefonnummeret ditt og meldingen din). De som åpner lenken, ser fornavnet
                    ditt, og du ser fornavnene til dem som blir med.</li>
                <li><strong>Stempelkort og «Mitt BESTI-år»:</strong> regnes ut fra timene du har hatt. Vi lagrer bare
                    når en bedrift har gitt deg belønningen.</li>
                <li><strong>Bedriftens kundekort:</strong> bedriften ser en oversikt over dine timer hos den (antall
                    besøk, «møtte ikke», neste time) og kan skrive et eget notat om deg, for eksempel «liker kort på
                    sidene». Notatet ser bare bedriften og de ansatte den har gitt egen innlogging. Bedriften er
                    ansvarlig for det den skriver.</li>
                <li><strong>Bedrifter:</strong> bedriftsnavn, organisasjonsnummer, adresse, telefon, e-post til
                    innlogging (vises aldri) og eventuelt til kunder, nettside, beskrivelse, ansatte (navn),
                    åpningstider, logo, bilder, blokkert tid og eventuelt hvor dere hørte om BESTI. Ansatte som får
                    egen innlogging, bruker en vanlig BESTI-konto, og bedriften lagrer e-postadressen de ble invitert
                    med.</li>
                <li><strong>Administrasjon:</strong> når BESTI godkjenner eller stenger en bedrift, skjuler en
                    vurdering eller hjelper en kunde, lagrer vi hva som ble gjort, når, av hvem i BESTI og hvorfor.</li>
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
                <li>Favoritter, venteliste, «Tid for ny time?», «Snart tid!», BESTI sammen, stempelkort og varsler på
                    telefonen: funksjoner du selv velger å bruke (avtale, art. 6 nr. 1 b). Du kan slå dem av når som
                    helst.</li>
                <li>Bedriftens kundekort, notater, ansattes egen kalender og bedriftens kalender i egen app: bedriften
                    er behandlingsansvarlig og bruker dem for å gi deg timen. BESTI er databehandler for bedriften.</li>
                <li>Å skjule vurderinger som bryter <Link href="/vilkar#vurderinger">vilkårene</Link>, stenge
                    bedrifter ved misbruk og føre logg over det (berettiget interesse, art. 6 nr. 1 f). Du får
                    beskjed med grunnen og kan svare oss.</li>
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
                <li><strong>Bedriften du bestiller hos</strong> ser bestillingen din, og de ansatte den har gitt egen
                    innlogging ser sine egne timer. Bedriften kan også vise timene i sin egen kalenderapp (Google,
                    Apple eller Outlook) gjennom en hemmelig lenke. Da får den kalenderleverandøren navn, telefon og
                    melding for timene, etter bedriftens avtale med leverandøren.</li>
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
                <li><strong>Nettleserens push-tjeneste</strong> (Google, Apple, Mozilla eller Microsoft, etter hvilken
                    nettleser du bruker): bare hvis du slår på varsler på telefonen. Meldingen er kryptert, så bare
                    enheten din kan lese den. Tjenesten ser at det sendes en melding til enheten, og når.</li>
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
                <li><strong>Venteliste:</strong> til dagen er over. <strong>Favoritter og «Tid for ny time?»:</strong>
                    til du fjerner dem eller sletter kontoen. <strong>Varsler på telefonen:</strong> til du slår dem
                    av, logger ut på enheten eller nettleseren melder at adressen ikke virker lenger.</li>
                <li><strong>Bilder i vurderinger:</strong> slettes når du sletter kontoen, 3 år etter timen, eller
                    når BESTI fjerner bildet fordi det bryter vilkårene.</li>
                <li><strong>BESTI sammen:</strong> invitasjonen slettes 3 år etter tidspunktet. Bedriftens notat om
                    deg slettes når du ikke har vært hos bedriften på 3 år, når du sletter kontoen, eller når
                    bedriften slettes. Loggen over administrasjon slettes etter 3 år.</li>
                <li><strong>Kundekonto:</strong> til du sletter den. Når du sletter kontoen, blir kommende timer
                    avlyst (bedriften får beskjed), tidligere timer anonymiseres med en gang, varslene, favorittene,
                    ventelisten, påminnelsene, invitasjonene, bildene og bedriftenes notater om deg slettes, og
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
                lagrer ingenting i nettleseren din. Velger du «Lyd ved nye varsler», husker nettleseren det valget
                (lokal lagring), og varsler på telefonen bruker en «service worker» som bare viser meldingene. Vi
                bruker ikke reklame-cookies eller sporing på tvers av
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
