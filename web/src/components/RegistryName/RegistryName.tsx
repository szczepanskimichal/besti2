import { lookupBusinessRegistry } from "@/lib/api";

// Words every salon may have in its name, so they do not count as "the same name"
const COMMON = new Set(["as", "enk", "da", "ans", "sa", "og", "i", "the", "salong", "salon", "frisør", "frisor",
    "studio", "klinikk", "beauty", "hair", "hud", "hudpleie", "massasje", "negler", "spa"]);

function words(name: string): string[] {
    return name.toLowerCase().split(/[^a-zæøå0-9]+/).filter((w) => w.length >= 3 && !COMMON.has(w));
}

// "Klipp & Stil" and "KLIPP OG STIL AS" share a real word; "QA Salong" and "REGISTERENHETEN I BRØNNØYSUND" do not
export function sameName(typed: string, registry: string): boolean {
    const registryWords = new Set(words(registry));
    return words(typed).some((w) => registryWords.has(w));
}

// For BESTI before approving: the name in Brønnøysund next to the name the business typed. The org. number
// alone only proves that some active business has it, not that it is this one
export default async function RegistryName({ orgNumber, name }: { orgNumber: string; name: string }) {
    const result = await lookupBusinessRegistry(orgNumber);
    if (!result.ok) {
        // Not found, or deleted / bankrupt: worth a warning. The register not answering is not
        return result.message.includes("svarer ikke")
            ? <>Navn i Brønnøysund kunne ikke hentes nå. Sjekk lenken.</>
            : <strong>⚠️ {result.message}</strong>;
    }
    if (!result.business.name) {
        return <>Navn i Brønnøysund kunne ikke hentes nå. Sjekk lenken.</>;
    }
    const registryName = result.business.name;
    return (
        <>
            Navn i Brønnøysund: <strong>{registryName}</strong>
            {sameName(name, registryName)
                ? " ✓"
                : <strong> · ⚠️ Ikke samme navn som oppgitt. Sjekk at det er samme bedrift før du godkjenner.</strong>}
        </>
    );
}
