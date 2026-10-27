import {headers} from "next/headers";

// Shared by every API call: address, headers, timeout and turning answers into ApiResult

export const API_URL = process.env.API_URL ?? "http://localhost:5156";

// Forward the visitor's IP so the API limits attempts per person, not per web server.
// In production only cf-connecting-ip counts: Cloudflare sets it and a visitor cannot fake it
// (anyone can send their own x-forwarded-for, so that one is only used locally).
async function forwardedFor(): Promise<Record<string, string>> {
    const h = await headers();
    const ip = h.get("cf-connecting-ip")
        ?? (process.env.NODE_ENV === "production" ? undefined : h.get("x-forwarded-for")?.split(",")[0]?.trim());
    return ip ? { "X-Forwarded-For": ip } : {};
}

export const TIMEOUT_MS = 8000;

export const UPLOAD_TIMEOUT_MS = 30000; // a photo can take longer on a slow phone connection

export const GENERIC_ERROR = "Noe gikk galt. Prøv igjen.";

const NO_CONTACT = "Vi fikk ikke kontakt med BESTI akkurat nå. Prøv igjen om litt.";

export type ApiResult = { ok: true } | { ok: false; message: string };

type RequestOptions = {
    method?: string;
    token?: string;
    body?: unknown;
    forwardIp?: boolean;
};

// One place for the API address, headers, no-store and an 8 s timeout
export async function apiFetch(path: string, options: RequestOptions = {}): Promise<Response> {
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
export async function safeFetch(url: string, init: RequestInit): Promise<Response> {
    try {
        return await fetch(url, init);
    } catch {
        return Response.json({ message: NO_CONTACT }, { status: 503 });
    }
}

// { ok: true }, or { ok: false, message } with the API's own message (e.g. "Tiden er ikke lenger ledig")
export async function toResult(res: Response, fallback = GENERIC_ERROR): Promise<ApiResult> {

    if (res.ok) return { ok: true };

    const body = await res.json().catch(() => null);

    return { ok: false, message: body?.message ?? fallback };
}
