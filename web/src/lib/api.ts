import type {BusinessListItem, BusinessDetails, CreateBookingInput, CustomerBooking, PanelBooking} from "@/lib/types";
import {headers} from "next/headers";
import {cache} from "react";

const API_URL = process.env.API_URL ?? "http://localhost:5156";
// Forward the visitor's IP so the API limits attempts per person, not per web server.
// cf-connecting-ip is set by Cloudflare in production (cannot be faked); x-forwarded-for works locally.
async function forwardedFor(): Promise<Record<string, string>> {
    const h = await headers();
    const ip = h.get("cf-connecting-ip") ?? h.get("x-forwarded-for")?.split(",")[0]?.trim();
    return ip ? { "X-Forwarded-For": ip } : {};
}
const TIMEOUT_MS = 8000;
const GENERIC_ERROR = "Noe gikk galt. Prøv igjen.";
export type ApiResult = { ok: true } | { ok: false; message: string };
type RequestOptions = {
    method?: string;
    token?: string;
    body?: unknown;
    forwardIp?: boolean;
};
// One place for the API address, headers, no-store and an 8 s timeout
// (a hanging API shows the error page instead of a page that never loads)
async function apiFetch(path: string, options: RequestOptions = {}): Promise<Response> {
    const headers: Record<string, string> = {};
    if (options.body !== undefined) headers["Content-Type"] = "application/json";
    if (options.token) headers.Authorization = `Bearer ${options.token}`;
    if (options.forwardIp) Object.assign(headers, await forwardedFor());
    return fetch(`${API_URL}${path}`, {
        method: options.method ?? "GET",
        headers,
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
        cache: "no-store",
        signal: AbortSignal.timeout(TIMEOUT_MS),
    });
}
// { ok: true }, or { ok: false, message } with the API's own message (e.g. "Tiden er ikke lenger ledig")
async function toResult(res: Response, fallback = GENERIC_ERROR): Promise<ApiResult> {

    if (res.ok) return { ok: true };

    const body = await res.json().catch(() => null);

    return { ok: false, message: body?.message ?? fallback };
}

export async function getBusinesses(category?: string, city?: string, q?: string, date?: string, from?: string, to?: string): Promise<BusinessListItem[]> {    
    const params = new URLSearchParams();
    if (category) params.set("category", category);
    if (city) params.set("city", city);
    if (q) params.set("q", q);
    if (date) params.set("date", date);
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    
    const res = await apiFetch(`/api/businesses?${params}`);
    
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedrifter (${res.status})`);
    }

    return res.json();
}

export async function getBusiness(id: string): Promise<BusinessDetails | null> {
    const res = await apiFetch(`/api/businesses/${id}`);
    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }

    return res.json();
}

export async function getCities(): Promise<string[]> {
    const res = await apiFetch("/api/cities");

    if (!res.ok) {
        throw new Error(`Kunne ikke hente byer (${res.status})`);
    }

    return res.json();
}

export async function getAvailability(serviceId: string, employeeId: string, date: string): Promise<string[] | null> {
    const params = new URLSearchParams({serviceId, employeeId, date});
    const res = await apiFetch(`/api/availability?${params}`);
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente ledige tider (${res.status})`);
    }
    return res.json();
}
export async function createBooking(input: CreateBookingInput, token: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/bookings", { method: "POST", token, body: input, forwardIp: true }));
}

export async function registerBusiness(input: Record<string, string>): Promise<ApiResult> {
    return toResult(await apiFetch("/api/businesses/register", { method: "POST", body: input, forwardIp: true }));
}

export async function login(email: string, password: string): Promise<{ accessToken: string; expiresIn: number } | null | "tooMany"> {
    const res = await apiFetch("/api/auth/login", { method: "POST", body: { email, password }, forwardIp: true });
// Too many attempts: the API's rate limiter answered 429
    if (res.status === 429) {
        return "tooMany";
    }
    if (!res.ok) {
        return null;
    }
    return res.json();
}

export async function getPanelBookings(token: string): Promise<PanelBooking[] | null> {
    const res = await apiFetch("/api/panel/bookings", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bestillinger (${res.status})`);
    }
    return res.json();
}

export async function changeBookingStatus(token: string, bookingId: string, action: "bekreft" | "avlys"): Promise<boolean> {
    const res = await apiFetch(`/api/panel/bookings/${bookingId}/${action}`, { method: "POST", token });
    return res.ok;
}

export async function getPanelBusiness(token: string): Promise<BusinessDetails | null> {
    const res = await apiFetch("/api/panel/business", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }
    return res.json();
}

export async function addService(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/services", { method: "POST", token, body: input }));
}


export async function updateService(token: string, serviceId: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/services/${serviceId}`, { method: "PUT", token, body: input }));
}

export async function deleteService(token: string, serviceId: string): Promise<boolean> {
    const res = await apiFetch(`/api/panel/services/${serviceId}`, { method: "DELETE", token });
    return res.ok;
}


export async function updateProfile(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/business", { method: "PUT", token, body: input }));
}


export async function updateOpeningHours(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/opening-hours", { method: "PUT", token, body: input }));
}

export async function uploadLogo(token: string, file: File): Promise<ApiResult> {
    const body = new FormData();
    body.append("file", file);
// Not apiFetch: FormData sets its own Content-Type (with the file boundary)
    const res = await fetch(`${API_URL}/api/panel/logo`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body,
        cache: "no-store",
        signal: AbortSignal.timeout(TIMEOUT_MS),
    });
    return toResult(res);
}

// Always "ok": the API never says whether the e-mail exists (so nobody can check who has an account)
export async function forgotPassword(email: string): Promise<void> {
    await apiFetch("/api/auth/forgotPassword", { method: "POST", body: { email }, forwardIp: true });
}

export async function resetPassword(email: string, resetCode: string, newPassword: string): Promise<boolean> {
    const res = await apiFetch("/api/auth/resetPassword", { method: "POST", body: { email, resetCode, newPassword }, forwardIp: true });
    return res.ok;
}

export async function addEmployee(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/employees", { method: "POST", token, body: input }));
}

export async function updateEmployee(token: string, employeeId: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/employees/${employeeId}`, { method: "PUT", token, body: input }));
}

// Returns the API message too, because the last employee cannot be deleted
export async function deleteEmployee(token: string, employeeId: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/employees/${employeeId}`, { method: "DELETE", token }));
}

export async function registerCustomer(input: { fullName: string; email: string; password: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/account/register", { method: "POST", body: input, forwardIp: true }));
}

export async function confirmEmail(userId: string, code: string): Promise<boolean> {
    const res = await apiFetch("/api/account/confirm-email", { method: "POST", body: { userId, code }, forwardIp: true });
    return res.ok;
}

export type Me = {
    email: string;
    fullName: string | null;
    isBusiness: boolean;
};

// cache(): the header and the page ask for the same user, but the API is called once per request
export const getMe = cache(async (token: string): Promise<Me | null> => {
    const res = await apiFetch("/api/me", { token });
    return res.ok ? res.json() : null;
});

// Customer: own bookings; null = not logged in (token expired)
export async function getMyBookings(token: string): Promise<CustomerBooking[] | null> {
    const res = await apiFetch("/api/me/bookings", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bestillinger (${res.status})`);
    }
    return res.json();
}

// Customer cancels an own booking; returns the API's message when it fails
export async function cancelMyBooking(token: string, bookingId: string): Promise<ApiResult> {
    const res = await apiFetch(`/api/me/bookings/${bookingId}/avbestill`, { method: "POST", token });
    return toResult(res, "Kunne ikke avbestille. Prøv igjen.");
}