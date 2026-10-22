import type {BusinessPage, BusinessDetails, CreateBookingInput, CustomerBooking, PanelBooking, BusinessReviews, TimeOff, NewTime, NotificationList} from "@/lib/types";
import {headers} from "next/headers";
import {cache} from "react";

const API_URL = process.env.API_URL ?? "http://localhost:5156";
// Forward the visitor's IP so the API limits attempts per person, not per web server.
// In production only cf-connecting-ip counts: Cloudflare sets it and a visitor cannot fake it
// (anyone can send their own x-forwarded-for, so that one is only used locally).
async function forwardedFor(): Promise<Record<string, string>> {
    const h = await headers();
    const ip = h.get("cf-connecting-ip")
        ?? (process.env.NODE_ENV === "production" ? undefined : h.get("x-forwarded-for")?.split(",")[0]?.trim());
    return ip ? { "X-Forwarded-For": ip } : {};
}
const TIMEOUT_MS = 8000;
const UPLOAD_TIMEOUT_MS = 30000; // a photo can take longer on a slow phone connection
const GENERIC_ERROR = "Noe gikk galt. Prøv igjen.";
const NO_CONTACT = "Vi fikk ikke kontakt med BESTI akkurat nå. Prøv igjen om litt.";
export type ApiResult = { ok: true } | { ok: false; message: string };
type RequestOptions = {
    method?: string;
    token?: string;
    body?: unknown;
    forwardIp?: boolean;
};
// One place for the API address, headers, no-store and an 8 s timeout
async function apiFetch(path: string, options: RequestOptions = {}): Promise<Response> {
    const headers: Record<string, string> = {};
    if (options.body !== undefined) headers["Content-Type"] = "application/json";
    if (options.token) headers.Authorization = `Bearer ${options.token}`;
    if (options.forwardIp) Object.assign(headers, await forwardedFor());
    return safeFetch(`${API_URL}${path}`, {
        method: options.method ?? "GET",
        headers,
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
        cache: "no-store",
        signal: AbortSignal.timeout(TIMEOUT_MS),
    });
}
// API down or too slow: answer like a failed request (503) instead of throwing. Forms then show
// a message and keep what was typed; pages that need the data still show the error page.
async function safeFetch(url: string, init: RequestInit): Promise<Response> {
    try {
        return await fetch(url, init);
    } catch {
        return Response.json({ message: NO_CONTACT }, { status: 503 });
    }
}
// { ok: true }, or { ok: false, message } with the API's own message (e.g. "Tiden er ikke lenger ledig")
async function toResult(res: Response, fallback = GENERIC_ERROR): Promise<ApiResult> {

    if (res.ok) return { ok: true };

    const body = await res.json().catch(() => null);

    return { ok: false, message: body?.message ?? fallback };
}

export async function getBusinesses(
    category?: string, 
    city?: string, 
    q?: string, 
    date?: string, 
    from?: string, 
    to?: string, 
    lat?: string, 
    lng?: string, 
    km?: string, 
    page?: string
    ): Promise<BusinessPage> {
    const params = new URLSearchParams();
    if (category) params.set("category", category);
    if (city) params.set("city", city);
    if (q) params.set("q", q);
    if (date) params.set("date", date);
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    if (lat) params.set("lat", lat);
    if (lng) params.set("lng", lng);
    if (km) params.set("maxKm", km);
    if (page) params.set("page", page);

    const res = await apiFetch(`/api/businesses?${params}`);
    
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedrifter (${res.status})`);
    }

    return res.json();
}

export const getBusiness = cache(async (id: string): Promise<BusinessDetails | null> => {
    const res = await apiFetch(`/api/businesses/${id}`);
    if (res.status === 404) {
        return null;
    }

    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }

    return res.json();
});

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
    const res = await apiFetch("/api/bookings", { method: "POST", token, body: input, forwardIp: true });
    // No answer in time: the booking may still have been saved, so do not just say "try again"
    if (res.status === 503) {
        return { ok: false, message: "Vi fikk ikke svar i tide. Sjekk Min Besti om timen er bestilt før du prøver igjen." };
    }
    return toResult(res);
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

// from/to = "2026-10-20" days for the calendar; without them: upcoming bookings and the last 14 days
export async function getPanelBookings(token: string, from?: string, to?: string): Promise<PanelBooking[] | null> {
    const params = from && to ? `?${new URLSearchParams({ from, to })}` : "";
    const res = await apiFetch(`/api/panel/bookings${params}`, { token });

    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bestillinger (${res.status})`);
    }
    return res.json();
}
// Business adds a visit itself (phone call, walk-in); date and time in Oslo
export async function createManualBooking(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/bookings", { method: "POST", token, body: input }));
}
// "Flytt": another time and/or employee; date and time in Oslo
export async function moveBooking(token: string, bookingId: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/bookings/${bookingId}/flytt`, { method: "POST", token, body: input }));
}
// Blocked time touching the days from-to ("2026-10-20"); null = not logged in as a business
export async function getPanelTimeOff(token: string, from: string, to: string): Promise<TimeOff[] | null> {
    const res = await apiFetch(`/api/panel/timeoff?${new URLSearchParams({ from, to })}`, { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente blokkert tid (${res.status})`);
    }
    return res.json();
}

// overlapping = visits already in the blocked time (they are kept, the calendar warns about them)
export async function createTimeOff(token: string, input: object): Promise<{ ok: true; overlapping: number } | { ok: false; message: string }> {
    const res = await apiFetch("/api/panel/timeoff", { method: "POST", token, body: input });
    const body = await res.json().catch(() => null);
    return res.ok
        ? { ok: true, overlapping: body?.overlappingBookings ?? 0 }
        : { ok: false, message: body?.message ?? GENERIC_ERROR };
}

export async function deleteTimeOff(token: string, id: string): Promise<boolean> {
    const res = await apiFetch(`/api/panel/timeoff/${id}`, { method: "DELETE", token });
    return res.ok;
}
// "tooLate" = the API refused because the visit has started (409), so the panel can say why
// message = optional text for the customer when cancelling ("Beklager, Ingrid er syk")
export async function changeBookingStatus(token: string, bookingId: string, action: "bekreft" | "avlys", message?: string): Promise<"ok" | "tooLate" | "error"> {
    const body = action === "avlys" ? { message: message?.trim() || null } : undefined;
    const res = await apiFetch(`/api/panel/bookings/${bookingId}/${action}`, { method: "POST", token, body });
    if (res.ok) return "ok";
    return res.status === 409 ? "tooLate" : "error";
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
    const res = await safeFetch(`${API_URL}/api/panel/logo`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body,
        cache: "no-store",
        signal: AbortSignal.timeout(TIMEOUT_MS),
    });
    return toResult(res);
}

export async function uploadGalleryImage(token: string, file: File): Promise<ApiResult> {
    const body = new FormData();
    body.append("file", file);
// Not apiFetch: FormData sets its own Content-Type (with the file boundary)
    const res = await safeFetch(`${API_URL}/api/panel/gallery`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body,
        cache: "no-store",
        signal: AbortSignal.timeout(UPLOAD_TIMEOUT_MS),
    });
    return toResult(res);
}

export async function deleteGalleryImage(token: string, imageId: string): Promise<boolean> {
    const res = await apiFetch(`/api/panel/gallery/${imageId}`, { method: "DELETE", token });
    return res.ok;
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
    phone: string | null; // from "Min profil"; fills in the booking form
    isBusiness: boolean;
};

// null = not logged in (401/403). Other errors (API down) are thrown, so a logged-in business
// sees the error page instead of being sent to the login page again and again
export const getMe = cache(async (token: string): Promise<Me | null> => {
    const res = await apiFetch("/api/me", { token });
    if (res.ok) return res.json();
    if (res.status === 401 || res.status === 403) return null;
    throw new Error(`Kunne ikke hente brukeren (${res.status})`);
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

// "Ny tid": the moved visit behind the one-time link (null = wrong or used link)
export async function getNewTime(token: string): Promise<NewTime | null> {
    const res = await apiFetch(`/api/ny-tid?${new URLSearchParams({ token })}`, { forwardIp: true });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente den nye tiden (${res.status})`);
    }
    return res.json();
}

// The customer's answer: from the link (token) or from Min Besti (logged in)
export async function answerNewTime(token: string, accept: boolean): Promise<ApiResult> {
    return toResult(await apiFetch("/api/ny-tid", { method: "POST", body: { token, accept }, forwardIp: true }));
}

export async function answerMyNewTime(token: string, bookingId: string, accept: boolean): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/me/bookings/${bookingId}/ny-tid`, { method: "POST", token, body: { accept } }));
}

// Customer reviews a past visit; returns the API's message when it fails
export async function createReview(
    token: string,
    input: { bookingId: string; stars: number; comment: string },
): Promise<ApiResult> {
    const res = await apiFetch("/api/me/reviews", { method: "POST", token, body: input });
    return toResult(res, "Kunne ikke lagre vurderingen. Prøv igjen.");
}

const NO_REVIEWS: BusinessReviews = {average: null, count: 0, reviews: []};

// Public: reviews of a business with the average
export async function getBusinessReviews(businessId: string): Promise<BusinessReviews> {
    // No reviews section rather than a broken business page when the API fails or is too slow
    try {
        const res = await apiFetch(`/api/businesses/${businessId}/reviews`);
        return res.ok ? await res.json() : NO_REVIEWS;
    } catch {
        return NO_REVIEWS;
    }
}

// Business panel: the owner's reviews (null = not logged in as a business)
export async function getPanelReviews(token: string): Promise<BusinessReviews | null> {
    const res = await apiFetch("/api/panel/reviews", {token});
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente vurderinger (${res.status})`);
    }
    return res.json();
}

export async function replyToReview(token: string, reviewId: string, reply: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/reviews/${reviewId}/reply`, {method: "POST", token, body: {reply}}));
}
// The bell (🔔); null = not logged in or the API failed (the header must never break because of it)
export async function getNotifications(token: string): Promise<NotificationList | null> {
    const res = await apiFetch("/api/notifications", { token });
    return res.ok ? res.json() : null;
}

// "Merk alle som lest"
export async function markNotificationsRead(token: string): Promise<void> {
    await apiFetch("/api/notifications/read", { method: "POST", token });
}

// A click on one notification: marks it as read and returns where it leads (null = not found)
export async function markNotificationRead(token: string, id: string): Promise<string | null> {
    const res = await apiFetch(`/api/notifications/${id}/read`, { method: "POST", token });
    if (!res.ok) {
        return null;
    }
    const body = await res.json();
    return body.link ?? "/varsler";
}

export async function deleteNotification(token: string, id: string): Promise<void> {
    await apiFetch(`/api/notifications/${id}`, { method: "DELETE", token });
}

// The live bell: an open connection from the API (Server-Sent Events). Not apiFetch: it must stay open,
// so no 8 s timeout; it closes when the browser leaves (signal)
export async function openNotificationStream(token: string, signal: AbortSignal): Promise<Response> {
    return fetch(`${API_URL}/api/notifications/stream`, {
        headers: {Authorization: `Bearer ${token}`},
        cache: "no-store",
        signal,
    });
}
// "Min profil" (customer)
export type MyProfile = { fullName: string; email: string; phone: string | null };

export async function getMyProfile(token: string): Promise<MyProfile | null> {
    const res = await apiFetch("/api/me/profile", { token });
    return res.ok ? res.json() : null;
}

export async function updateMyProfile(token: string, input: { fullName: string; phone: string | null }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/profile", { method: "PUT", token, body: input }));
}

export async function changeMyPassword(token: string, input: { currentPassword: string; newPassword: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/password", { method: "POST", token, body: input }));
}

// "Slett konto"; confirmed with the password
export async function deleteMyAccount(token: string, password: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/delete", { method: "POST", token, body: { password } }));
}
