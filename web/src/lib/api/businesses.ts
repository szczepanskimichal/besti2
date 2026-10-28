import type {BusinessPage, BusinessDetails, BusinessApproval, FreeDay} from "@/lib/types";
import {cache} from "react";
import {apiFetch, toResult, type ApiResult} from "./client";

// Public business data: search, business page, free times, registration and BESTI approval

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
    page?: string,
    sort?: string,
    minRating?: string
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
    if (sort) params.set("sort", sort);
    if (minRating) params.set("minRating", minRating);

    // forwardIp: the API limits searches per visitor, not for the whole site at once
    const res = await apiFetch(`/api/businesses?${params}`, { forwardIp: true });

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

// The public page by its short address (bestiapp.no/klipp-og-stil-frisor-gjovik)
export const getBusinessBySlug = cache(async (slug: string): Promise<BusinessDetails | null> => {
    // Only a-z, 0-9 and dashes can be a business; /favicon.png or /wp-admin never reach the API
    if (!/^[a-z0-9-]{1,80}$/.test(slug)) {
        return null;
    }
    const res = await apiFetch(`/api/businesses/by-slug/${slug}`);
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

// "Velg dag": 7 days of free times from a date; without employeeId = anyone free (Hvem som helst)
export async function getFreeDays(serviceId: string, employeeId: string | undefined, from: string, units = 1): Promise<FreeDay[] | null> {
    const params = new URLSearchParams({serviceId, from, units: String(units)});
    if (employeeId) {
        params.set("employeeId", employeeId);
    }
    // forwardIp: the API limits free-time lookups per visitor, not for the whole site at once
    const res = await apiFetch(`/api/availability/days?${params}`, { forwardIp: true });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente ledige tider (${res.status})`);
    }
    return res.json();
}

export async function registerBusiness(input: Record<string, string | boolean>): Promise<ApiResult> {
    return toResult(await apiFetch("/api/businesses/register", { method: "POST", body: input, forwardIp: true }));
}

// "Hent fra Brønnøysund": name and address of an active business in Enhetsregisteret
export type RegistryBusiness = { name: string | null; address: string | null; postalCode: string | null; city: string | null };

export async function lookupBusinessRegistry(orgNumber: string): Promise<{ ok: true; business: RegistryBusiness } | { ok: false; message: string }> {
    const res = await apiFetch(`/api/brreg/${encodeURIComponent(orgNumber)}`, { forwardIp: true });
    const body = await res.json().catch(() => null);
    return res.ok
        ? { ok: true, business: body }
        : { ok: false, message: body?.message ?? body?.detail ?? "Brønnøysundregistrene svarer ikke akkurat nå. Fyll ut feltene selv." };
}

// BESTI's approval link for a new business; null when the link is wrong or already used
export async function getBusinessApproval(token: string): Promise<BusinessApproval | null> {
    const res = await apiFetch(`/api/godkjenn?${new URLSearchParams({ token })}`, { forwardIp: true });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente bedriften (${res.status})`);
    }
    return res.json();
}

export async function approveBusiness(token: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/godkjenn", { method: "POST", body: { token }, forwardIp: true }));
}

export async function rejectBusiness(token: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/godkjenn/avvis", { method: "POST", body: { token }, forwardIp: true }));
}
