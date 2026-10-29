import type {StaffInviteInfo, StaffPlace} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";

// /ansatt: an employee's own calendar with their own BESTI account

// The link in the invitation e-mail (null = unknown or expired)
export async function getStaffInvite(code: string): Promise<StaffInviteInfo | null> {
    const res = await apiFetch(`/api/staff/invites/${encodeURIComponent(code)}`, { forwardIp: true });
    return res.ok ? res.json() : null;
}

export async function acceptStaffInvite(token: string, code: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/staff/invites/${encodeURIComponent(code)}/accept`,
        { method: "POST", token, forwardIp: true }));
}

// null = not logged in
export async function getStaffSchedule(token: string, from: string, days: number): Promise<StaffPlace[] | null> {
    const res = await apiFetch(`/api/staff/schedule?from=${from}&days=${days}`, { token });
    if (res.status === 401 || res.status === 403) return null;
    if (!res.ok) throw new Error(`Kunne ikke hente timene (${res.status})`);
    return res.json();
}

export async function leaveStaffPlace(token: string, employeeId: string): Promise<boolean> {
    const res = await apiFetch(`/api/staff/places/${encodeURIComponent(employeeId)}/leave`, { method: "POST", token });
    return res.ok;
}
