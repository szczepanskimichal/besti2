using besti2.Domain.Enums;

namespace besti2.Application.Businesses;

// A ready service the owner can add with one click (name, length and a normal price in Norway, incl. mva)
public record ServiceTemplateDto(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal? PriceNok,
    string PriceType);

// "Forslag for din bransje" on /panel/tjenester: a new business has its services in a minute.
// Only suggestions: the owner changes price and length before adding, and edits them later like any service
public static class ServiceTemplates
{
    public const int MaxPerRequest = 30;

    private static ServiceTemplateDto Fixed(string name, int minutes, decimal price, string? description = null) =>
        new(name, description, minutes, price, nameof(PriceType.FastPris));

    // One hour per unit: the customer picks how many hours when booking
    private static ServiceTemplateDto Hourly(string name, decimal price, string? description = null) =>
        new(name, description, 60, price, nameof(PriceType.PerTime));

    private static ServiceTemplateDto ByAgreement(string name, int minutes, string? description = null) =>
        new(name, description, minutes, null, nameof(PriceType.EtterAvtale));

    // Craftsmen are booked for a visit first; the job itself is priced after it
    private static ServiceTemplateDto Inspection() =>
        Fixed("Befaring", 60, 0, "Gratis og uforpliktende. Vi ser på jobben og gir deg et pristilbud.");

    private static readonly Dictionary<BusinessCategory, ServiceTemplateDto[]> Templates = new()
    {
        [BusinessCategory.Frisor] =
        [
            Fixed("Dameklipp", 60, 699, "Klipp, vask og føn"),
            Fixed("Herreklipp", 30, 449),
            Fixed("Barneklipp (under 12 år)", 30, 349),
            Fixed("Farge, kort hår", 90, 1190),
            Fixed("Striper med folie", 120, 1490),
            Fixed("Vask og føn", 30, 399)
        ],
        [BusinessCategory.Barber] =
        [
            Fixed("Herreklipp", 30, 449),
            Fixed("Skjeggtrim", 20, 249),
            Fixed("Klipp og skjegg", 45, 599),
            Fixed("Barbering med varmt håndkle", 30, 399),
            Fixed("Barneklipp (under 12 år)", 30, 299)
        ],
        [BusinessCategory.Skjonnhet] =
        [
            Fixed("Ansiktsbehandling", 60, 990),
            Fixed("Forming og farging av bryn", 30, 399),
            Fixed("Farging av vipper", 20, 249),
            Fixed("Voksing, legger", 45, 599),
            Fixed("Makeup", 60, 899)
        ],
        [BusinessCategory.Negler] =
        [
            Fixed("Gellakk, hender", 45, 549),
            Fixed("Gelenegler, nytt sett", 90, 899),
            Fixed("Påfyll, gelenegler", 75, 749),
            Fixed("Manikyr", 45, 599),
            Fixed("Fjerning av gellakk", 20, 249)
        ],
        [BusinessCategory.Vipper] =
        [
            Fixed("Vippeextensions, nytt sett", 120, 990),
            Fixed("Påfyll, vippeextensions", 60, 590),
            Fixed("Vippeløft", 60, 699),
            Fixed("Brynslaminering", 45, 599)
        ],
        [BusinessCategory.Massasje] =
        [
            Fixed("Klassisk massasje, 30 min", 30, 549),
            Fixed("Klassisk massasje, 60 min", 60, 899),
            Fixed("Idrettsmassasje", 60, 949),
            Fixed("Hot stone-massasje", 75, 1090)
        ],
        [BusinessCategory.Spa] =
        [
            Fixed("Spabehandling", 90, 1490),
            Fixed("Ansiktsbehandling", 60, 990),
            Fixed("Kroppspeeling", 45, 690)
        ],
        [BusinessCategory.Tatovering] =
        [
            Fixed("Konsultasjon", 30, 0, "Vi snakker om motiv, plassering og pris."),
            Fixed("Liten tatovering", 60, 1200),
            Hourly("Tatovering", 1500)
        ],
        [BusinessCategory.Fotpleie] =
        [
            Fixed("Fotpleie", 60, 790),
            Fixed("Medisinsk fotpleie", 60, 890),
            Fixed("Negleklipp, føtter", 30, 450)
        ],
        [BusinessCategory.PersonligTrener] =
        [
            Fixed("PT-time", 60, 890),
            Fixed("Introtime", 60, 690, "Vi blir kjent og lager en plan sammen."),
            Fixed("PT for to", 60, 1190)
        ],
        [BusinessCategory.Yoga] =
        [
            Fixed("Yogatime", 60, 250),
            Fixed("Privat yogatime", 60, 890)
        ],
        [BusinessCategory.Bilverksted] =
        [
            Fixed("Service, personbil", 120, 2490, "Deler og olje kommer i tillegg."),
            Hourly("Feilsøking og reparasjon", 1150),
            Fixed("Bremsesjekk", 30, 590)
        ],
        [BusinessCategory.Dekkskift] =
        [
            Fixed("Dekkskift, personbil", 30, 499),
            Fixed("Dekkskift med balansering", 45, 799),
            Fixed("Dekkskift, SUV og varebil", 45, 649)
        ],
        [BusinessCategory.EuKontroll] =
        [
            Fixed("EU-kontroll, personbil", 45, 990),
            Fixed("Etterkontroll", 30, 390)
        ],
        [BusinessCategory.Bilpleie] =
        [
            Fixed("Utvendig vask", 60, 699),
            Fixed("Innvendig rens", 90, 1290),
            Fixed("Polering og voks", 180, 2990)
        ],
        [BusinessCategory.Trafikkskole] =
        [
            Fixed("Kjøretime (45 min)", 45, 840),
            Fixed("Dobbel kjøretime", 90, 1680)
        ],
        [BusinessCategory.Bygg] = [Inspection(), Hourly("Snekkerarbeid", 750)],
        [BusinessCategory.Rorlegger] = [Inspection(), Hourly("Rørleggerarbeid", 1100)],
        [BusinessCategory.Elektriker] =
        [
            Inspection(),
            Hourly("Elektrikerarbeid", 1050),
            Fixed("El-kontroll, bolig", 90, 2490)
        ],
        [BusinessCategory.Maler] = [Inspection(), Hourly("Malerarbeid", 650)],
        [BusinessCategory.Flislegger] = [Inspection(), Hourly("Flislegging", 750)],
        [BusinessCategory.Taktekker] = [Inspection(), ByAgreement("Takreparasjon", 120)],
        [BusinessCategory.Laasesmed] =
        [
            Fixed("Låseåpning", 30, 1290),
            Fixed("Bytte av lås", 60, 1490, "Selve låsen kommer i tillegg.")
        ],
        [BusinessCategory.Rengjoring] =
        [
            Hourly("Hjemmevask", 450),
            Hourly("Flyttevask", 550),
            Hourly("Vindusvask", 450)
        ],
        [BusinessCategory.Flyttehjelp] =
        [
            Inspection(),
            Hourly("Flyttehjelp, to personer og bil", 990)
        ],
        [BusinessCategory.Hage] =
        [
            Hourly("Hagearbeid", 550),
            Fixed("Plenklipp", 60, 590),
            Hourly("Hekkeklipp", 590)
        ],
        [BusinessCategory.Dyrlege] =
        [
            Fixed("Konsultasjon", 30, 690),
            Fixed("Vaksinering", 20, 590),
            Fixed("Kloklipp", 15, 250)
        ],
        [BusinessCategory.Hundefrisor] =
        [
            Fixed("Bad og føn, liten hund", 60, 590),
            Fixed("Full klipp, liten hund", 90, 890),
            Fixed("Full klipp, stor hund", 150, 1290),
            Fixed("Kloklipp", 15, 150)
        ],
        [BusinessCategory.Fotograf] =
        [
            Fixed("Portrettfotografering", 60, 1490),
            Fixed("Familiefotografering", 90, 2490),
            ByAgreement("Bryllup, planleggingsmøte", 60)
        ],
        [BusinessCategory.Skredder] =
        [
            Fixed("Prøving og måltaking", 30, 0),
            Fixed("Legge opp bukse", 15, 290),
            Fixed("Bytte glidelås", 15, 390)
        ],
        [BusinessCategory.Undervisning] =
        [
            Fixed("Prøvetime", 30, 0),
            Fixed("Privattime", 60, 600),
            Fixed("Leksehjelp", 45, 450)
        ]
    };

    // Annet (and a category without suggestions yet) gets an empty list
    public static IReadOnlyList<ServiceTemplateDto> For(BusinessCategory category) =>
        Templates.TryGetValue(category, out var templates) ? templates : [];
}
