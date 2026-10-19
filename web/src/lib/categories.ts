// Groups only exist on the web: the API knows just the flat category value.
// Order here = order in the dropdowns. color = logo background (dark enough for white text).
type CategoryGroup = {
    label: string;
    color: string;
    categories: Record<string, string>;
};

export const categoryGroups: CategoryGroup[] = [    {
        label: "Skjønnhet og velvære",
        color: "#be185d",
        categories: {
            Frisor: "Frisør",
            Barber: "Barber",
            Skjonnhet: "Hudpleie og kosmetikk",
            Negler: "Negler",
            Vipper: "Vipper og bryn",
            Massasje: "Massasje",
            Spa: "Spa",
            Tatovering: "Tatovering og piercing",
        },
    },
    {
        label: "Helse",
        color: "#047857",
        categories: {
            Fysioterapi: "Fysioterapi",
            Kiropraktor: "Kiropraktor",
            Naprapat: "Naprapat",
            Psykolog: "Psykolog",
            Tannlege: "Tannlege",
            Lege: "Lege",
            Optiker: "Optiker",
            Fotpleie: "Fotpleie",
            Helse: "Annen helse",
        },
    },
    {
        label: "Trening",
        color: "#b91c1c",
        categories: {
            PersonligTrener: "Personlig trener",
            Yoga: "Yoga og pilates",
        },
    },
    {
        label: "Bil",
        color: "#334155",
        categories: {
            Bilverksted: "Bilverksted",
            Dekkskift: "Dekkskift og dekkhotell",
            EuKontroll: "EU-kontroll",
            Bilpleie: "Bilvask og bilpleie",
            Trafikkskole: "Trafikkskole",
        },
    },
    {
        label: "Hjem og håndverk",
        color: "#c2410c",
        categories: {
            Bygg: "Snekker og bygg",
            Rorlegger: "Rørlegger",
            Elektriker: "Elektriker",
            Maler: "Maler",
            Flislegger: "Flislegger",
            Taktekker: "Taktekker",
            Laasesmed: "Låsesmed",
            Rengjoring: "Rengjøring",
            Flyttehjelp: "Flyttehjelp",
            Hage: "Hage og snømåking",
        },
    },
    {
        label: "Dyr",
        color: "#92400e",
        categories: {
            Dyrlege: "Dyrlege",
            Hundefrisor: "Hundefrisør og dyrepleie",
        },
    },
    {
        label: "Annet",
        color: "#475569",
        categories: {
            Fotograf: "Fotograf",
            Skredder: "Skredder og skomaker",
            Undervisning: "Kurs og leksehjelp",
            Annet: "Annet",
        },
    },
];

// For the dropdowns: one optgroup per group
export const categoryOptionGroups = categoryGroups.map((group) => ({
    label: group.label,
    options: Object.entries(group.categories).map(([value, label]) => ({ value, label })),
}));

export function getCategoryLabel(category: string): string {
    for (const group of categoryGroups) {
        const label = group.categories[category];
        if (label) return label;
    }
    return category;
}

// Logo color of the category's group (grey for unknown values)
export function getCategoryColor(category: string): string {
    const group = categoryGroups.find((g) => category in g.categories);
    return group?.color ?? "#475569";
}
