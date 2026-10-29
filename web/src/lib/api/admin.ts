import type {AdminAction, AdminBusinessDetails, AdminBusinessRow, AdminCustomer, AdminHealth, AdminOverview, AdminReasons, AdminReview} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";
// /admin: only for the e-mails in Admin:Emails on the server. null = not an admin (or not logged in)

async function adminGet<T>(path: string, token: string): Promise<T | null> {
    const res = await apiFetch(path, { token });
    if (res.status === 401 || res.status === 403 || res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente ${path} (${res.status})`);
    }
    return res.json();
}

export function getAdminOverview(token: string): Promise<AdminOverview | null> {
    return adminGet("/api/admin/overview", token);
}

export function getAdminBusinesses(token: string, status?: string, q?: string): Promise<AdminBusinessRow[] | null> {
    const params = new URLSearchParams({ ...(status ? { status } : {}), ...(q ? { q } : {}) });
    return adminGet(`/api/admin/businesses?${params}`, token);
}

export function getAdminBusiness(token: string, id: string): Promise<AdminBusinessDetails | null> {
    return adminGet(`/api/admin/businesses/${encodeURIComponent(id)}`, token);
}

export function getAdminReviews(token: string): Promise<AdminReview[] | null> {
    return adminGet("/api/admin/reviews", token);
}

export async function setAdminNote(token: string, id: string, note: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/businesses/${encodeURIComponent(id)}/note`,
        { method: "PUT", token, body: { note } }));
}

// "godkjenn" or "avvis" a business that waits for approval
export async function decideBusiness(token: string, id: string, approve: boolean): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/businesses/${encodeURIComponent(id)}/${approve ? "approve" : "reject"}`,
        { method: "POST", token }), "Bedriften venter ikke lenger på godkjenning.");
}

export function getAdminHiddenReviews(token: string): Promise<AdminReview[] | null> {
    return adminGet("/api/admin/reviews?skjult=true", token);
}

export function getAdminReasons(token: string): Promise<AdminReasons | null> {
    return adminGet("/api/admin/reasons", token);
}

export async function hideReview(token: string, id: string, reason: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/reviews/${encodeURIComponent(id)}/hide`,
        { method: "POST", token, body: { reason } }));
}

export async function showReview(token: string, id: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/reviews/${encodeURIComponent(id)}/show`, { method: "POST", token }));
}

export async function suspendBusiness(token: string, id: string, reason: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/businesses/${encodeURIComponent(id)}/suspend`,
        { method: "POST", token, body: { reason } }));
}

export async function reopenBusiness(token: string, id: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/businesses/${encodeURIComponent(id)}/reopen`, { method: "POST", token }));
}

export function findAdminCustomers(token: string, q: string): Promise<AdminCustomer[] | null> {
    return adminGet(`/api/admin/customers?q=${encodeURIComponent(q)}`, token);
}

export async function resendConfirmation(token: string, userId: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/admin/customers/${encodeURIComponent(userId)}/resend-confirmation`,
        { method: "POST", token }));
}

export function getAdminHealth(token: string): Promise<AdminHealth | null> {
    return adminGet("/api/admin/health", token);
}

export function getAdminLog(token: string): Promise<AdminAction[] | null> {
    return adminGet("/api/admin/log", token);
}
