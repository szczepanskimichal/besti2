const categoryLabels: Record<string, string> = {
    Frisor: "Frisør",
    Barber: "Barber",
    Skjonnhet: "Skjønnhet",
    Bygg: "Bygg og anlegg",
    Rorlegger: "Rørlegger",
    Elektriker: "Elektriker",
    Helse: "Helse",
    Annet: "Annet",
};

export function getCategoryLabel(category: string): string {
    return categoryLabels[category] ?? category;
}