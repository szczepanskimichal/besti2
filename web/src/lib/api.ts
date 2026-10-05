import type { BusinessListItem, BusinessDetails } from "@/lib/types";

const API_URL = process.env.API_URL ?? "http://localhost:5156";

export async function getBusinesses(): Promise<BusinessListItem[]> {
    const res = await fetch(`${API_URL}/api/businesses`, { cache: "no-store" });

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedrifter (${res.status})`);
    }

    return res.json();
}

export async function getBusiness(id: string): Promise<BusinessDetails | null> {
    const res = await fetch(`${API_URL}/api/businesses/${id}`, { cache: "no-store" });

    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }

    return res.json();
}

export async function getAvailability(serviceId: string, employeeId: string, date: string): Promise<string[] | null> {
    const params = new URLSearchParams({ serviceId, employeeId, date });
    const res = await fetch(`${API_URL}/api/availability?${params}`, { cache: "no-store" });

    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente ledige tider (${res.status})`);
    }

    return res.json();
}

export async function createBooking(input: CreateBookingInput): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/bookings`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(input),
        cache: "no-store",
    });

    if (res.ok) {
        return { ok: true };
    }

    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}