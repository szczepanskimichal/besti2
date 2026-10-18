import type { BusinessListItem, BusinessDetails, CreateBookingInput, PanelBooking } from "@/lib/types";

const API_URL = process.env.API_URL ?? "http://localhost:5156";

export async function getBusinesses(category?: string, city?: string): Promise<BusinessListItem[]> {
    const params = new URLSearchParams();
    if (category) params.set("category", category);
    if (city) params.set("city", city);

    const res = await fetch(`${API_URL}/api/businesses?${params}`, { cache: "no-store" });

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
export async function getCities(): Promise<string[]> {
    const res = await fetch(`${API_URL}/api/cities`, { cache: "no-store" });

    if (!res.ok) {
        throw new Error(`Kunne ikke hente byer (${res.status})`);
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
export async function registerBusiness(input: Record<string, string>): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/businesses/register`, {
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

export async function login(email: string, password: string): Promise<{ accessToken: string; expiresIn: number } | null> {
    const res = await fetch(`${API_URL}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
        cache: "no-store",
    });

    if (!res.ok) {
        return null;
    }

    return res.json();
}

// 
export async function getPanelBookings(token: string): Promise<PanelBooking[] | null> {
    const res = await fetch(`${API_URL}/api/panel/bookings`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
    });

    if (res.status === 401 || res.status === 403) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bestillinger (${res.status})`);
    }

    return res.json();
}

export async function changeBookingStatus(token: string, bookingId: string, action: "bekreft" | "avlys"): Promise<boolean> {
    const res = await fetch(`${API_URL}/api/panel/bookings/${bookingId}/${action}`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
    });
    return res.ok;
}

export async function getPanelBusiness(token: string): Promise<BusinessDetails | null> {
    const res = await fetch(`${API_URL}/api/panel/business`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
    });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }
    return res.json();
}

export async function addService(token: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/services`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return { ok: true };
    }
    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}


export async function updateService(token: string, serviceId: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/services/${serviceId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return { ok: true };
    }
    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}

export async function deleteService(token: string, serviceId: string): Promise<boolean> {
    const res = await fetch(`${API_URL}/api/panel/services/${serviceId}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
    });
    return res.ok;
}


export async function updateProfile(token: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/business`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return { ok: true };
    }
    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}

export async function updateOpeningHours(token: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/opening-hours`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return { ok: true };
    }
    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}