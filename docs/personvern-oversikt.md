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
Sist gjennomgått: 2026-10-28

## Behandlinger

| Behandling | Personopplysninger | Hvem | Formål | Grunnlag (GDPR) | Lagringstid | Hvor i koden |
|---|---|---|---|---|---|---|
| Kundekonto | navn, e-post, passord-hash, telefon (valgfritt) | kunder | konto, innlogging, fylle ut bestilling | 6.1 b (avtale) | til kontoen slettes | `AppUser`, `ProfileService`, `CustomerAccountService` |
| Bestilling | navn, telefon, e-post, tjeneste, ansatt, tid, status, melding | kunder | gjennomføre timen; bedriften ser bestillingen | 6.1 b | 5 år etter timen, så anonymisert | `Booking`, `BookingService`, `BookingAnonymizer` (`RetentionYears = 5`) |
| Ny tid (Flytt) | lenketoken (kun SHA-256), forrige tidspunkt | kunder | kunden godtar eller avslår ny tid | 6.1 b | til kunden svarer / timen avlyses | `Booking.NewTimeTokenHash`, `PreviousStartUtc` |
| Vurderinger | fornavn, stjerner, kommentar | kunder | vise vurderinger offentlig | 6.1 f (berettiget interesse) | navnet byttes til «Tidligere kunde» 5 år etter timen eller ved slettet konto; slettes med bedriften | `Review`, `ReviewService`, `BookingAnonymizer`, `ProfileService.DeleteAsync` |
| «Møtte ikke» | status på timen, antall per kunde (e-post) hos samme bedrift | kunder | bedriften planlegger; kunden får e-post og kan protestere | 6.1 f | som bestillingen | `BookingStatus.MotteIkke`, `BookingService` (teller bare innen bedriften), `BookingEmails.SendNoShowAsync` |
| Påminnelse | navn, e-post, tid, tjeneste | kunder | e-post dagen før timen | 6.1 b | som bestillingen | `BookingReminder`, `BookingReminderJob`, `Booking.NoReminder` |
| Varsler (bjelle) | tekst om timer, lenke | kunder, bedrifter | informere om endringer | 6.1 b | til slettet, maks 7 dager (kunde) / 14 dager (bedrift) | `Notification`, `NotificationService` (`CustomerDays`, `BusinessDays`), `BookingAnonymizationJob` |
| Nyhetsbrev-samtykke | ikke i bruk: alltid nei (spørsmålet ble fjernet før beta) | – | – | – | som bestillingen | `Booking.MarketingConsent` |
| Bedriftskonto | bedriftsnavn, org.nr, adresse, telefon, e-post (innlogging + til kunder), nettside, ansattes navn, logo og bilder | bedrifter, ansatte | vise bedriften og ta imot bestillinger | 6.1 b | til bedriften sletter den («Slett bedrift»: alt slettes, også timer, vurderinger og bildefiler) | `Business`, `Employee`, `BusinessService`, `BusinessRegistrationService.DeleteOwnBusinessAsync` |
| Bedriftssjekk | org.nr | bedrifter | hindre falske bedrifter | 6.1 f | ikke lagret hos Brreg | `BrregBusinessRegistry` |
| Godkjenning av bedrift | bedriftens opplysninger i e-post til BESTI, lenketoken (kun SHA-256), godkjent-tidspunkt | bedrifter | hindre at noen registrerer en annens bedrift | 6.1 f | token til godkjent; tidspunkt så lenge bedriften finnes | `BusinessRegistrationService` (`ApproveAsync`), `Business.ApprovedUtc` |
| Databehandleravtale | tidspunkt for godkjenning | bedrifter | dokumentere avtalen (art. 28) | 6.1 c / 6.1 b | så lenge bedriften finnes | `Business.DataProcessingAcceptedUtc`, `/vilkar#databehandleravtale` |
| Sikkerhet | IP-adresse | alle | hindre misbruk (rate limiting), feilsøking | 6.1 f | i minnet minutter; serverlogger maks 5 × 10 MB per tjeneste | `Program.cs` (rate limiter), `deploy/docker-compose.prod.yml` (logging) |
| Innlogging | innloggingstoken i informasjonskapsel | alle | holde brukeren innlogget | 6.1 b | 14 dager; ugyldig straks passordet endres | `Program.cs` (`BearerTokenExpiration`, security stamp), `lib/auth.ts` |
| Sikkerhetskopi | hele databasen, bilder, innloggingsnøkler | alle | gjenopprette etter feil eller angrep | 6.1 f | 14 dager (på serveren og i Hetzner Storage Box, Tyskland) | `deploy/backup.sh` |

## Mottakere og leverandører

| Mottaker | Hva | Hvor | Avtale |
|---|---|---|---|
| Bedriften kunden bestiller hos | bestillingen | Norge | — (egen behandlingsansvarlig) |
| Hetzner Online GmbH (servere, database, sikkerhetskopier) | hele databasen og bildene | EU (servere i Finland, sikkerhetskopier i Tyskland) | databehandleravtale (DPA) |
| Brevo / Sendinblue SAS (e-post) | e-postadresse, navn, innhold i e-post | EU (Frankrike) | DPA |
| Cloudflare, Inc. (DNS, beskyttelse, videreformidling av trafikk) | IP-adresse og all trafikk til nettsiden | USA / globalt | DPA, EU–US Data Privacy Framework + standardkontraktsbestemmelser (SCC) |
| Kartverket (Geonorge) | bedriftens adresse | Norge | offentlig API, ingen kundedata |
| Brønnøysundregistrene | bedriftens org.nr | Norge | offentlig API, ingen kundedata |

Overføring ut av EU/EØS: bare Cloudflare (USA), under Data Privacy Framework og SCC. Ingen sporing, analyse eller annonsenettverk. Én nødvendig informasjonskapsel (innlogging).

## Den registrertes rettigheter – hvor i tjenesten

- Innsyn og dataportabilitet: Min Besti → Profil → «Last ned mine data» (`GET /api/me/export`)
- Retting: Profil (navn, telefon)
- Sletting: Profil → «Slett konto» (`POST /api/me/delete`); bedrift: Bedriftsprofil → «Slett bedrift» (`POST /api/panel/delete`)
- Klage: Datatilsynet

## Rutiner

- Personopplysninger tas ikke ut av databasen uten behov (ingen eksport til regneark o.l.).
- Ved sikkerhetsbrudd: vurder og meld til Datatilsynet innen 72 timer (art. 33).
- Før vi tar i bruk en ny leverandør: sjekk at den har databehandleravtale og lagrer i EU/EØS (eller har gyldig overføringsgrunnlag), og varsle bedriftene før den tas i bruk (databehandleravtalen i vilkårene).
- Ved sikkerhetsbrudd som gjelder kunder av en bedrift: varsle bedriften innen 48 timer.

## Åpne punkter

- Bedrifter innen helse (psykolog, fysioterapi): bestillinger kan avsløre helseforhold. Vurder egne regler før slike bedrifter tas inn i større skala.
