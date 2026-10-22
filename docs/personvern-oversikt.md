# Personvern – oversikt over behandlinger (BESTI)

Intern protokoll over behandlingsaktiviteter (GDPR art. 30). Én kilde til sannhet: den offentlige teksten på
`/personvern` (web/src/app/personvern/page.tsx) skal alltid stemme med denne filen.

**Oppdater denne filen og /personvern i samme pull request når vi:**
- samler inn en ny type personopplysning (særlig helse eller andre særlige kategorier),
- tar i bruk en ny leverandør som får opplysninger (betaling, SMS, analyse, ny server),
- sender opplysninger ut av EU/EØS,
- bruker opplysninger til et nytt formål (f.eks. markedsføring),
- endrer hvor lenge noe lagres.

Behandlingsansvarlig: Michał Szczepański (BESTI). Kontakt: personvern@bestiapp.no
Sist gjennomgått: 2026-10-22

## Behandlinger

| Behandling | Personopplysninger | Hvem | Formål | Grunnlag (GDPR) | Lagringstid | Hvor i koden |
|---|---|---|---|---|---|---|
| Kundekonto | navn, e-post, passord-hash, telefon (valgfritt) | kunder | konto, innlogging, fylle ut bestilling | 6.1 b (avtale) | til kontoen slettes | `AppUser`, `ProfileService`, `CustomerAccountService` |
| Bestilling | navn, telefon, e-post, tjeneste, ansatt, tid, status, melding | kunder | gjennomføre timen; bedriften ser bestillingen | 6.1 b | 5 år etter timen, så anonymisert | `Booking`, `BookingService`, `BookingAnonymizer` (`RetentionYears = 5`) |
| Ny tid (Flytt) | lenketoken (kun SHA-256), forrige tidspunkt | kunder | kunden godtar eller avslår ny tid | 6.1 b | til kunden svarer / timen avlyses | `Booking.NewTimeTokenHash`, `PreviousStartUtc` |
| Vurderinger | fornavn, stjerner, kommentar | kunder | vise vurderinger offentlig | 6.1 f (berettiget interesse) | til bedriften slettes; ved slettet konto: «Tidligere kunde» | `Review`, `ReviewService`, `ProfileService.DeleteAsync` |
| Varsler (bjelle) | tekst om timer, lenke | kunder, bedrifter | informere om endringer | 6.1 b | til slettet, maks 7 dager (kunde) / 14 dager (bedrift) | `Notification`, `NotificationService` (`CustomerDays`, `BusinessDays`), `BookingAnonymizationJob` |
| Nyhetsbrev-samtykke | ja/nei og tidspunkt | kunder | markedsføring (ikke i bruk ennå) | 6.1 a (samtykke) | som bestillingen | `Booking.MarketingConsent` |
| Bedriftskonto | bedriftsnavn, org.nr, adresse, telefon, e-post, ansattes navn, bilder | bedrifter, ansatte | vise bedriften og ta imot bestillinger | 6.1 b | til bedriften avslutter | `Business`, `Employee`, `BusinessService` |
| Bedriftssjekk | org.nr | bedrifter | hindre falske bedrifter | 6.1 f | ikke lagret hos Brreg | `BrregBusinessRegistry` |
| Sikkerhet | IP-adresse | alle | hindre misbruk (rate limiting), feilsøking | 6.1 f | i minnet minutter; serverlogger kort tid | `Program.cs` (rate limiter), logger |

## Mottakere og leverandører

| Mottaker | Hva | Hvor | Avtale |
|---|---|---|---|
| Bedriften kunden bestiller hos | bestillingen | Norge | — (egen behandlingsansvarlig) |
| Server/hosting (planlagt: Hetzner) | hele databasen | EU (Tyskland/Finland) | databehandleravtale (DPA) |
| E-post (planlagt: Brevo) | e-postadresse, navn, innhold i e-post | EU (Frankrike) | DPA |
| Kartverket (Geonorge) | bedriftens adresse | Norge | offentlig API, ingen kundedata |
| Brønnøysundregistrene | bedriftens org.nr | Norge | offentlig API, ingen kundedata |

Ingen overføring ut av EU/EØS. Ingen sporing, analyse eller annonsenettverk. Én nødvendig informasjonskapsel (innlogging).

## Den registrertes rettigheter – hvor i tjenesten

- Innsyn og dataportabilitet: Min Besti → Profil → «Last ned mine data» (`GET /api/me/export`)
- Retting: Profil (navn, telefon)
- Sletting: Profil → «Slett konto» (`POST /api/me/delete`)
- Klage: Datatilsynet

## Rutiner

- Personopplysninger tas ikke ut av databasen uten behov (ingen eksport til regneark o.l.).
- Ved sikkerhetsbrudd: vurder og meld til Datatilsynet innen 72 timer (art. 33).
- Før vi tar i bruk en ny leverandør: sjekk at den har databehandleravtale og lagrer i EU/EØS.

## Åpne punkter

- Databehandleravtale mellom BESTI og bedriftene (som del av vilkårene) – kommer sammen med vilkår og betaling.
- Bedrifter innen helse (psykolog, fysioterapi): bestillinger kan avsløre helseforhold. Vurder egne regler før slike bedrifter tas inn i større skala.
