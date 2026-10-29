import type {BusinessDetails, PanelBooking, ServiceTemplate, TimeOff} from "@/lib/types";
import {API_URL, UPLOAD_TIMEOUT_MS, GENERIC_ERROR, apiFetch, safeFetch, toResult, type ApiResult} from "./client";
// The business panel: calendar, blocked time, services, employees, profile, photos

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
export async function changeBookingStatus(token: string, bookingId: string,     action: "bekreft" | "avlys" | "motte-ikke", message?: string): Promise<"ok" | "tooLate" | "error"> {
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

// "Del og få kunder": the QR code to the business page as a data: address for <img> and download links;
// null = not logged in as a business
export async function getPanelQr(token: string, format: "svg" | "png"): Promise<string | null> {
    const res = await apiFetch(`/api/panel/qr?format=${format}`, { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke lage QR-koden (${res.status})`);
    }
    const base64 = Buffer.from(await res.arrayBuffer()).toString("base64");
    return `data:image/${format === "svg" ? "svg+xml" : "png"};base64,${base64}`;
}

export async function addService(token: string, input: object): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/services", { method: "POST", token, body: input }));
}

// "Forslag for din bransje": suggestions for the business's industry, without the services it already has
export async function getServiceTemplates(token: string): Promise<ServiceTemplate[]> {
    const res = await apiFetch("/api/panel/service-templates", { token });
    return res.ok ? res.json() : [];
}

// The chosen suggestions in one go: all are saved, or none
export async function addServices(token: string, input: object[]): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/services/batch", { method: "POST", token, body: input }));
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
        signal: AbortSignal.timeout(UPLOAD_TIMEOUT_MS),
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
// "Slett bedrift": the business with all its data and the owner account; confirmed with the password
export async function deleteMyBusiness(token: string, password: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/delete", { method: "POST", token, body: { password }, forwardIp: true }));
}

// "Kalender på telefonen": the secret part of the calendar address, or null when it is turned off
export async function getCalendarFeedToken(token: string): Promise<string | null> {
    const res = await apiFetch("/api/panel/calendar-feed", { token });
    if (!res.ok) {
        return null;
    }
    return (await res.json()).token ?? null;
}

// Turns it on, or makes a new address (the old one stops working)
export async function createCalendarFeed(token: string): Promise<boolean> {
    return (await apiFetch("/api/panel/calendar-feed", { method: "POST", token })).ok;
}

export async function turnOffCalendarFeed(token: string): Promise<boolean> {
    return (await apiFetch("/api/panel/calendar-feed", { method: "DELETE", token })).ok;
}

// The calendar file for Google / Apple / Outlook; null = unknown address or turned off
export async function getCalendarFeed(feedToken: string): Promise<string | null> {
    const res = await apiFetch(`/api/calendar/${encodeURIComponent(feedToken)}`);
    return res.ok ? res.text() : null;
}
