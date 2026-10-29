import type {CreateBookingInput, CustomerBooking, NewTime, RebookReminder, WaitlistEntry} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";

// The customer's bookings: book, Min Besti, cancel, answer a new time

export async function createBooking(input: CreateBookingInput, token: string): Promise<ApiResult> {
    const res = await apiFetch("/api/bookings", { method: "POST", token, body: input, forwardIp: true });
    // No answer in time: the booking may still have been saved, so do not just say "try again"
    if (res.status === 503) {
        return { ok: false, message: "Vi fikk ikke svar i tide. Sjekk Min Besti om timen er bestilt før du prøver igjen." };
    }
    return toResult(res);
}

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

// Customer cancels an own booking; message = optional reason for the business
// Returns the API's message when it fails
export async function cancelMyBooking(token: string, bookingId: string, message?: string): Promise<ApiResult> {
    const body = { message: message?.trim() || null };
    const res = await apiFetch(`/api/me/bookings/${bookingId}/avbestill`, { method: "POST", token, body });
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

// "Si fra hvis det blir ledig": wait for a free time with this service that day (employeeId null = anyone)
export async function joinWaitlist(token: string, input: { serviceId: string; employeeId: string | null; date: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/waitlist", { method: "POST", token, body: input, forwardIp: true }));
}

// Min Besti: the days I wait for; empty when the API fails (the page must still show)
export async function getMyWaitlist(token: string): Promise<WaitlistEntry[]> {
    const res = await apiFetch("/api/waitlist/mine", { token });
    return res.ok ? res.json() : [];
}

export async function leaveWaitlist(token: string, id: string): Promise<void> {
    await apiFetch(`/api/waitlist/${encodeURIComponent(id)}`, { method: "DELETE", token });
}

// "Tid for ny time?": weeks = 0 turns it off
export async function setRebookReminder(token: string, input: { serviceId: string; employeeId: string | null; weeks: number }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/rebook-reminders", { method: "PUT", token, body: input }));
}

export async function getMyRebookReminders(token: string): Promise<RebookReminder[]> {
    const res = await apiFetch("/api/rebook-reminders/mine", { token });
    return res.ok ? res.json() : [];
}

export async function removeRebookReminder(token: string, id: string): Promise<void> {
    await apiFetch(`/api/rebook-reminders/${encodeURIComponent(id)}`, { method: "DELETE", token });
}
