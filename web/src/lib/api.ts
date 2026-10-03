import type { BusinessListItem } from "@/lib/types";

const API_URL = process.env.API_URL ?? "http://localhost:5156";

export async function getBusinesses(): Promise<BusinessListItem[]> {
    const res = await fetch(`${API_URL}/api/businesses`, { cache: "no-store" });

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedrifter (${res.status})`);
    }

    return res.json();
}