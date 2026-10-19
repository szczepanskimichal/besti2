import type {BusinessListItem, BusinessDetails, CreateBookingInput, PanelBooking} from "@/lib/types";

const API_URL = process.env.API_URL ?? "http://localhost:5156";

export async function getBusinesses(category?: string, city?: string, q?: string): Promise<BusinessListItem[]> {
    const params = new URLSearchParams();
    if (category) params.set("category", category);
    if (city) params.set("city", city);
    if (q) params.set("q", q);


    const res = await fetch(`${API_URL}/api/businesses?${params}`, {cache: "no-store"});

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedrifter (${res.status})`);
    }

    return res.json();
}

export async function getBusiness(id: string): Promise<BusinessDetails | null> {
    const res = await fetch(`${API_URL}/api/businesses/${id}`, {cache: "no-store"});

    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }

    return res.json();
}

export async function getCities(): Promise<string[]> {
    const res = await fetch(`${API_URL}/api/cities`, {cache: "no-store"});

    if (!res.ok) {
        throw new Error(`Kunne ikke hente byer (${res.status})`);
    }

    return res.json();
}

export async function getAvailability(serviceId: string, employeeId: string, date: string): Promise<string[] | null> {
    const params = new URLSearchParams({serviceId, employeeId, date});
    const res = await fetch(`${API_URL}/api/availability?${params}`, {cache: "no-store"});

    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente ledige tider (${res.status})`);
    }

    return res.json();
}

export async function createBooking(input: CreateBookingInput, token: string): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/bookings`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify(input),
        cache: "no-store",
    });

    if (res.ok) {
        return {ok: true};
    }

    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function registerBusiness(input: Record<string, string>): Promise<{ ok: true } | {
    ok: false;
    message: string
}> {
    const res = await fetch(`${API_URL}/api/businesses/register`, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify(input),
        cache: "no-store",
    });

    if (res.ok) {
        return {ok: true};
    }

    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function login(email: string, password: string): Promise<{
    accessToken: string;
    expiresIn: number
} | null> {
    const res = await fetch(`${API_URL}/api/auth/login`, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({email, password}),
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
        headers: {Authorization: `Bearer ${token}`},
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
        headers: {Authorization: `Bearer ${token}`},
        cache: "no-store",
    });
    return res.ok;
}

export async function getPanelBusiness(token: string): Promise<BusinessDetails | null> {
    const res = await fetch(`${API_URL}/api/panel/business`, {
        headers: {Authorization: `Bearer ${token}`},
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
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}


export async function updateService(token: string, serviceId: string, input: object): Promise<{ ok: true } | {
    ok: false;
    message: string
}> {
    const res = await fetch(`${API_URL}/api/panel/services/${serviceId}`, {
        method: "PUT",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function deleteService(token: string, serviceId: string): Promise<boolean> {
    const res = await fetch(`${API_URL}/api/panel/services/${serviceId}`, {
        method: "DELETE",
        headers: {Authorization: `Bearer ${token}`},
        cache: "no-store",
    });
    return res.ok;
}


export async function updateProfile(token: string, input: object): Promise<{ ok: true } | {
    ok: false;
    message: string
}> {
    const res = await fetch(`${API_URL}/api/panel/business`, {
        method: "PUT",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function updateOpeningHours(token: string, input: object): Promise<{ ok: true } | {
    ok: false;
    message: string
}> {
    const res = await fetch(`${API_URL}/api/panel/opening-hours`, {
        method: "PUT",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function uploadLogo(token: string, file: File): Promise<{ ok: true } | { ok: false; message: string }> {
    const body = new FormData();
    body.append("file", file);
    const res = await fetch(`${API_URL}/api/panel/logo`, {
        method: "POST",
        headers: {Authorization: `Bearer ${token}`},
        body,
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const json = await res.json().catch(() => null);
    return {ok: false, message: json?.message ?? "Noe gikk galt. Prøv igjen."};
}

// Always "ok": the API never says whether the e-mail exists (so nobody can check who has an account)
export async function forgotPassword(email: string): Promise<void> {
    await fetch(`${API_URL}/api/auth/forgotPassword`, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({email}),
        cache: "no-store",
    });
}

export async function resetPassword(email: string, resetCode: string, newPassword: string): Promise<boolean> {
    const res = await fetch(`${API_URL}/api/auth/resetPassword`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, resetCode, newPassword }),
        cache: "no-store",
    });
    return res.ok;
}

export async function addEmployee(token: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/employees`, {
        method: "POST",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}
export async function updateEmployee(token: string, employeeId: string, input: object): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/employees/${employeeId}`, {
        method: "PUT",
        headers: {"Content-Type": "application/json", Authorization: `Bearer ${token}`},
        body: JSON.stringify(input),
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

// Returns the API message too, because the last employee cannot be deleted
export async function deleteEmployee(token: string, employeeId: string): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/panel/employees/${employeeId}`, {
        method: "DELETE",
        headers: {Authorization: `Bearer ${token}`},
        cache: "no-store",
    });
    if (res.ok) {
        return {ok: true};
    }
    const body = await res.json().catch(() => null);
    return {ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen."};
}

export async function registerCustomer(input: { fullName: string; email: string; password: string }): Promise<{ ok: true } | { ok: false; message: string }> {
    const res = await fetch(`${API_URL}/api/account/register`, {
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

export async function confirmEmail(userId: string, code: string): Promise<boolean> {
    const res = await fetch(`${API_URL}/api/account/confirm-email`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, code }),
        cache: "no-store",
    });
    return res.ok;
}

export type Me = {
    email: string;
    fullName: string | null;
    isBusiness: boolean;
};

export async function getMe(token: string): Promise<Me | null> {
    const res = await fetch(`${API_URL}/api/me`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
    });
    return res.ok ? res.json() : null;
}