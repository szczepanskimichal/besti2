import type { Metadata } from "next";
import Link from "next/link";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Vilkår for bruk",
};

// Must match what the app really does (booking rules, cancellation, "Møtte ikke", reviews, Google login).
// The data processing agreement (#databehandleravtale) is accepted by every business at sign-up:
// a real change to it is announced to the businesses by e-mail first
export default function TermsPage() {
    return (
        <main className={styles.main}>
            <h1>Vilkår for bruk</h1>
            <p className={styles.updated}>Sist oppdatert: 29. oktober 2026</p>
            <h2>Om tjenesten</h2>
            <p>
                BESTI er en tjeneste for å finne bedrifter og bestille time på nett. BESTI drives av Michał
                Szczepański i Gjøvik. Kontakt: <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>.
            </p>
            <p>
                Avtalen om selve tjenesten (for eksempel en hårklipp eller en befaring) inngås mellom deg og
                bedriften. BESTI er ikke part i den avtalen, og spørsmål om pris, kvalitet og reklamasjon tas med
                bedriften.
            </p>
            <h2>Konto</h2>
            <ul>
                <li>Du trenger en konto for å bestille. Du må være minst 13 år. Er du under 18, bør du ha en
                    foresattes godkjenning før du bestiller noe som koster penger.</li>
                <li>Du kan opprette konto med e-post og passord, eller med «Fortsett med Google». Hold passordet
                    ditt hemmelig, og si fra til oss hvis du tror noen andre har brukt kontoen din.</li>
                <li>Én konto er personlig. Du kan slette den når som helst under Min Besti → Profil.</li>
            </ul>
            <h2>For kunder</h2>
            <ul>
                <li>Oppgi riktig navn, telefon og e-post når du bestiller.</li>
                <li>Du kan bestille tidligst 60 minutter før timen, og ha opptil 3 kommende timer hos samme bedrift
                    om gangen.</li>
                <li>Noen bedrifter bekrefter timen med en gang, andre bekrefter den selv. Du får beskjed på e-post,
                    og timen står i Min Besti.</li>
                <li>Bedriften kan foreslå en ny tid. Du velger selv om du godtar den eller ikke. Bedriften kan også
                    avlyse timen, og du får da beskjed med en gang.</li>
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
                <li>Er du uenig med en bedrift om tjenesten, kan du få gratis hjelp fra{" "}
                    <a href="https://www.forbrukerradet.no">Forbrukerrådet</a>.</li>
            </ul>
            <h2 id="vurderinger">Vurderinger</h2>
            <ul>
                <li>Du kan gi én vurdering for hver time du har hatt, og bare etter at timen er over.</li>
                <li>Skriv om din egen opplevelse, ærlig og saklig. Ikke skriv personangrep, trusler, hatefulle eller
                    ulovlige ytringer, reklame eller opplysninger om andre personer, for eksempel ansattes helse.</li>
                <li>Vurderingen vises med fornavnet ditt. 3 år etter timen, eller når du sletter kontoen, vises den som
                    «Tidligere kunde».</li>
                <li>Bedriften kan svare offentlig, men kan ikke slette, endre eller velge bort vurderinger. Snittet
                    regnes av alle vurderinger, og en bedrift får stjerner i søket først når den har minst
                    3 vurderinger.</li>
                <li>Bedrifter skal ikke skrive, kjøpe eller be om falske vurderinger, verken av seg selv eller av
                    andre. Det er også forbudt etter markedsføringsloven.</li>
                <li>BESTI kan fjerne vurderinger som bryter disse reglene. Mener du at en vurdering bryter reglene,
                    skriv til <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>.</li>
            </ul>
            <h2>For bedrifter</h2>
            <ul>
                <li>Bare den som har rett til å representere bedriften, kan registrere den. Nye bedrifter blir sjekket
                    i Enhetsregisteret og godkjent av BESTI før kundene ser dem.</li>
                <li>Opplysningene om bedriften (navn, organisasjonsnummer, adresse, priser, åpningstider) skal være
                    riktige og oppdaterte.</li>
                <li>Bedriften får en fast adresse på BESTI (for eksempel bestiapp.no/salong-kari-gjovik), laget av
                    navn og by. Den endres ikke, slik at QR-koder og lenker du har delt, fortsetter å virke.</li>
                <li>Du kan bruke lenken, QR-koden og plakaten fra panelet («Del og få kunder») i egen markedsføring,
                    for eksempel i lokalet, på Google, Instagram og Facebook.</li>
                <li>Du gir BESTI rett til å vise opplysningene, logoen og bildene dine på BESTI og når lenken til
                    siden din vises andre steder (for eksempel forhåndsvisning på Facebook eller i Google), så lenge
                    bedriften har konto.</li>
                <li>Du er ansvarlig for tjenestene du tilbyr og for å følge lover og regler for din bransje.</li>
                <li>Driver du med helsetjenester (for eksempel psykolog eller fysioterapi), er du selv ansvarlig for
                    å vurdere om timebestilling via BESTI passer med taushetsplikten og kravene til
                    informasjonssikkerhet i din bransje. Be ikke kundene skrive helseopplysninger i meldingsfeltet.</li>
                <li>Du kan bare laste opp bilder du har rettigheter til. Personer på bildene må ha gitt samtykke.
                    Si fra til de ansatte at navnet deres vises på siden.</li>
                <li>Kundeopplysninger du får gjennom BESTI kan bare brukes til å gi kunden timen, ikke til
                    markedsføring uten kundens eget samtykke.</li>
                <li>Du velger selv om nye bestillinger bekreftes automatisk, og setter fristen for gratis
                    avbestilling. Valgene gjelder for alle timer bestilt via BESTI.</li>
                <li>Registrer «Møtte ikke» bare når kunden faktisk ikke kom. Feil registrering skal rettes med en gang
                    («Møtte»).</li>
                <li>Legger du inn en time for en kunde selv, sender du bare bekreftelse og påminnelse på e-post når
                    kunden har bedt om det.</li>
                <li>
                    Når kunder bestiller time hos deg, er du behandlingsansvarlig for kundenes opplysninger,
                    og BESTI er databehandler. Se <a href="#databehandleravtale">databehandleravtalen</a> under.
                </li>
                <li>BESTI kan fjerne innhold som er ulovlig eller bryter vilkårene, og stenge kontoer ved misbruk.
                    Hvis det ikke haster, sier vi fra først og gir deg mulighet til å rette det.</li>
                <li>Du kan slette bedriften når som helst i panelet (Bedriftsprofil → «Slett bedrift»). Kommende timer
                    må avlyses først, slik at kundene får beskjed.</li>
            </ul>
            <h2>Pris</h2>
            <p>
                BESTI er gratis for kunder. For bedrifter er tjenesten gratis i beta-perioden. Innfører vi priser,
                får bedriftene beskjed på e-post minst 30 dager før, og ingen bedrift betaler noe uten å ha godtatt
                prisen først.
            </p>
            <p>
                Noen funksjoner i panelet er merket «Pro» (for eksempel venteliste, «Tid for ny time?», stempelkort og
                statistikk for mer enn 30 dager). De er gratis i beta-perioden, men blir senere en del av en betalt
                pakke, BESTI Pro. Funksjonene uten Pro-merke er med i grunnpakken.
            </p>
            <h2 id="databehandleravtale">Databehandleravtale</h2>
            <p>
                Denne delen er databehandleravtalen mellom bedriften (behandlingsansvarlig) og BESTI
                (databehandler), jf. personvernforordningen artikkel 28. Bedriften godtar den når den
                registrerer seg, og den gjelder så lenge bedriften har konto hos BESTI.
            </p>
            <ul>
                <li><strong>Hvem det gjelder:</strong> bedriftens kunder (også kunder bedriften legger inn selv) og
                    bedriftens ansatte.</li>
                <li><strong>Hva behandles:</strong> opplysninger om bedriftens kunder i forbindelse med bestillinger
                    (navn, telefon, e-post, tjeneste, ansatt, tidspunkt, status inkludert «møtte ikke», kundens
                    melding og grunn for avbestilling, varsler om timen) og navn på bedriftens ansatte.</li>
                <li><strong>Hvor lenge:</strong> bestillinger anonymiseres automatisk 3 år etter timen. Varsler slettes
                    etter høyst 14 dager. Sikkerhetskopier slettes etter 14 dager.</li>
                <li><strong>Formål:</strong> å ta imot, vise, flytte og avlyse bestillinger og sende bekreftelser,
                    påminnelser og varsler på vegne av bedriften. BESTI bruker ikke opplysningene til egne formål,
                    som markedsføring.</li>
                <li><strong>Instrukser:</strong> BESTI behandler opplysningene bare etter bedriftens instrukser, slik de
                    følger av denne avtalen og av det bedriften gjør i tjenesten. Mener BESTI at en instruks er i
                    strid med personvernreglene, sier BESTI fra til bedriften.</li>
                <li><strong>Taushetsplikt og sikkerhet:</strong> bare personer som trenger det for å drifte tjenesten,
                    har tilgang, og de har taushetsplikt. Opplysningene beskyttes med kryptert forbindelse (HTTPS),
                    passord som bare lagres som hash, tilgangskontroll, begrensning av antall forsøk og daglige
                    sikkerhetskopier (artikkel 32).</li>
                <li><strong>Underleverandører:</strong> bedriften godtar underleverandørene som er merket
                    «databehandler for bedriftene» i{" "}
                    <Link href="/personvern#underleverandorer">personvernerklæringen</Link>. BESTI har
                    databehandleravtaler med dem på samme vilkår som her, og varsler bedriften på e-post før en ny
                    slik underleverandør tas i bruk, slik at bedriften kan protestere.</li>
                <li><strong>Overføring ut av EØS:</strong> skjer bare med gyldig grunnlag etter personvernforordningen
                    kapittel V (i dag bare Cloudflare, under EU–US Data Privacy Framework og EUs
                    standardkontraktsbestemmelser).</li>
                <li><strong>Hjelp til bedriften:</strong> BESTI hjelper bedriften med å svare på kunders krav om innsyn,
                    retting og sletting, og med sikkerhet, avvik og konsekvensvurderinger når det trengs.</li>
                <li><strong>Avvik:</strong> BESTI varsler bedriften uten ugrunnet opphold, og senest innen 48 timer,
                    hvis et brudd på personopplysningssikkerheten gjelder bedriftens kunder.</li>
                <li><strong>Kontroll:</strong> BESTI gir bedriften informasjonen som trengs for å vise at avtalen
                    følges, og bidrar til rimelige kontroller etter avtale.</li>
                <li><strong>Når avtalen slutter:</strong> når bedriften sletter kontoen («Slett bedrift»), sletter
                    BESTI kundeopplysningene med en gang, og de er borte fra sikkerhetskopiene innen 14 dager. Bedriften
                    kan først be om en kopi på <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>.</li>
                <li><strong>Endringer:</strong> vesentlige endringer i denne avtalen varsles bedriftene på e-post minst
                    30 dager før de gjelder.</li>
            </ul>
            <h2>Ansvar</h2>
            <p>
                Vi gjør vårt beste for at tjenesten fungerer, men kan ikke garantere at den alltid er
                tilgjengelig eller uten feil. BESTI er ikke ansvarlig for tjenester som utføres av bedriftene.
            </p>
            <p>
                Overfor bedrifter er BESTI ikke ansvarlig for indirekte tap, for eksempel tapt omsetning når tjenesten
                er nede, så lenge BESTI ikke har opptrådt grovt uaktsomt eller med vilje. Ingenting i vilkårene
                begrenser rettigheter du har som forbruker etter loven.
            </p>
            <h2>Endringer og kontakt</h2>
            <p>
                Vi kan oppdatere vilkårene. Vesentlige endringer varsles på nettsiden, og til bedriftene på e-post
                minst 30 dager før de gjelder. Spørsmål? Kontakt oss på{" "}
                <a href="mailto:post@bestiapp.no">post@bestiapp.no</a>. Norsk lov gjelder.
            </p>
        </main>
    );
}
