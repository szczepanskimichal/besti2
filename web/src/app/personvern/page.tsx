import type { Metadata } from "next";
import styles from "../legal.module.css";

export const metadata: Metadata = {
    title: "Personvernerklæring – BESTI",
};
export default function PrivacyPage() {
    return (
        <main className={styles.main}>
            <h1>Personvernerklæring</h1>
            <p className={styles.updated}>Sist oppdatert: 19. oktober 2026</p>
            <h2>Hvem er ansvarlig?</h2>
            <p>
                BESTI drives av Michał Szczepański (privatperson). Vi er behandlingsansvarlig for bedriftskontoer.
                Når du bestiller time, er bedriften du bestiller hos behandlingsansvarlig for
                opplysningene om bestillingen, og BESTI behandler dem på vegne av bedriften
                (databehandler). Kontakt: michalszczepanski07@gmail.com.
            </p>
            <h2>Hvilke opplysninger samler vi inn?</h2>
            <ul>
                <li>Når du bestiller time: navn, telefonnummer, e-post, valgt tjeneste og tidspunkt.</li>
                <li>Hvis du krysser av for nyhetsbrev: at du har samtykket, og når.</li>
                <li>For bedrifter: bedriftsnavn, organisasjonsnummer, adresse, telefon, e-post, beskrivelse og bilder (logo).</li>
                <li>For innlogging: e-post og passord. Passordet lagres kun kryptert (hash).</li>
            </ul>
            <h2>Hvorfor behandler vi opplysningene?</h2>
            <ul>
                <li>For å gjennomføre bestillingen og sende deg bekreftelse på e-post (avtale, GDPR art. 6 nr. 1 b).</li>
                <li>Bedriften du bestiller hos ser navn, telefon og e-post, slik at de kan gi deg timen.</li>
                <li>Nyhetsbrev sendes bare hvis du har samtykket (art. 6 nr. 1 a). Du kan trekke samtykket når som helst.</li>
                <li>For å vise bedriftens adresse på kart henter vi koordinater fra Kartverket.</li>
                <li>Bilder som lastes opp, blir forminsket og lagret uten skjulte bildedata (EXIF), som GPS-posisjon.</li>
            </ul>
            <h2>Hvem deler vi med?</h2>
            <p>
                Vi selger aldri personopplysninger. Vi bruker noen leverandører for å drive tjenesten,
                for eksempel for e-postutsending og drift av servere. De behandler bare opplysninger
                på våre vegne og etter våre instrukser.
            </p>
            <h2>Hvor lenge lagrer vi opplysningene?</h2>
            <p>
                Opplysninger om bestillinger lagres i 5 år etter timen, slik at bedriftene har
                historikk over tidligere bestillinger. Deretter slettes eller anonymiseres de.
                Bedriftskontoer lagres til kontoen slettes.
            </p>
            <h2>Informasjonskapsler (cookies)</h2>
            <p>
                Vi bruker bare én nødvendig informasjonskapsel, for innlogging i bedriftspanelet.
                Vi bruker ikke sporing eller reklame-cookies.
            </p>
            <h2>Dine rettigheter</h2>
            <p>
                Du har rett til innsyn, retting og sletting av dine opplysninger, og til å trekke
                tilbake samtykke. Kontakt oss på michalszczepanski07@gmail.com. Gjelder det en bestilling, kan du også
                kontakte bedriften direkte. Du kan også klage til Datatilsynet
                (datatilsynet.no).
            </p>
        </main>
    );
}
