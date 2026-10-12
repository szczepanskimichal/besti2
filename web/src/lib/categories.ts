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

export const categoryOptions = Object.entries(categoryLabels).map(
    ([value, label]) => ({ value, label }),
);
export function getCategoryLabel(category: string): string {
    return categoryLabels[category] ?? category;
}
