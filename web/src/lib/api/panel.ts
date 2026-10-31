import type {BookingDayCount, BusinessDetails, CustomerCard, CustomerImportResult, CustomerRow, PanelBooking, ServiceTemplate, StaffAccess, TimeOff} from "@/lib/types";
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

// "År": visits per day in one year (only one employee's with employeeId)
export async function getPanelBookingCounts(token: string, year: string, employeeId?: string): Promise<BookingDayCount[]> {
    const params = new URLSearchParams({ year });
    if (employeeId) {
        params.set("employeeId", employeeId);
    }
    const res = await apiFetch(`/api/panel/bookings/counts?${params}`, { token });
    return res.ok ? res.json() : [];
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

// "Kunder": the customers made from the bookings, with history and the business's own note
export async function getPanelCustomers(token: string, q?: string): Promise<CustomerRow[] | null> {
    const res = await apiFetch(`/api/panel/customers${q ? `?q=${encodeURIComponent(q)}` : ""}`, { token });
    if (res.status === 401 || res.status === 403) return null;
    if (!res.ok) throw new Error(`Kunne ikke hente kundene (${res.status})`);
    return res.json();
}

// null = no such customer at this business
export async function getPanelCustomer(token: string, key: string): Promise<CustomerCard | null> {
    const res = await apiFetch(`/api/panel/customers/${encodeURIComponent(key)}`, { token });
    if (res.status === 401 || res.status === 403 || res.status === 404) return null;
    if (!res.ok) throw new Error(`Kunne ikke hente kunden (${res.status})`);
    return res.json();
}

export async function setCustomerNote(token: string, key: string, note: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/customers/${encodeURIComponent(key)}/note`,
        { method: "PUT", token, body: { note } }));
}

// "Importer kunder": save = false checks the list and counts, nothing is saved
export async function importCustomers(token: string, rows: { line: number; name: string; phone: string;
    email: string; note: string }[], save: boolean, confirmed: boolean):
    Promise<{ ok: true; result: CustomerImportResult } | { ok: false; message: string }> {
    // Not apiFetch: a list of thousands of customers may take longer than 8 s to save
    const res = await safeFetch(`${API_URL}/api/panel/customers/import`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
        body: JSON.stringify({ rows, save, confirmed }),
        cache: "no-store",
        signal: AbortSignal.timeout(UPLOAD_TIMEOUT_MS),
    });
    if (res.ok) return { ok: true, result: await res.json() };
    const result = await toResult(res);
    return result.ok ? { ok: false, message: GENERIC_ERROR } : result;
}

// "Fjern fra Kunder": only an imported customer who has not booked here
export async function removeImportedCustomer(token: string, key: string): Promise<boolean> {
    const res = await apiFetch(`/api/panel/customers/${encodeURIComponent(key)}`, { method: "DELETE", token });
    return res.ok;
}

// The CSV file as the API made it (null = not allowed)
export async function exportBookingsCsv(token: string, from: string, to: string): Promise<Response | null> {
    const res = await apiFetch(`/api/panel/export/bookings?from=${from}&to=${to}`, { token });
    return res.ok ? res : null;
}

// Panel → Ansatte → "Egen innlogging"
export async function getStaffAccess(token: string): Promise<StaffAccess[]> {
    const res = await apiFetch("/api/panel/staff", { token });
    return res.ok ? res.json() : [];
}

export async function inviteStaff(token: string, employeeId: string, email: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/employees/${encodeURIComponent(employeeId)}/staff`,
        { method: "POST", token, body: { email }, forwardIp: true }));
}

export async function removeStaff(token: string, employeeId: string): Promise<boolean> {
    const res = await apiFetch(`/api/panel/employees/${encodeURIComponent(employeeId)}/staff`, { method: "DELETE", token });
    return res.ok;
}
