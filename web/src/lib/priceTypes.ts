// Mirrors the PriceType enum in besti2.Domain/Enums/PriceType.cs
const priceTypeLabels: Record<string, string> = {
    FastPris: "Fast pris",
    PerTime: "Per time",
    PerKvadratmeter: "Per kvadratmeter",
    PerLøpendeMeter: "Per løpende meter",
    EtterAvtale: "Pris etter avtale",
};

export const priceTypeOptions = Object.entries(priceTypeLabels).map(
    ([value, label]) => ({ value, label }),
);