import type {BusinessStats} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";
// "Statistikk" in the business panel, the Ukerapport switch and interest in BESTI Pro

// days = 7, 30, 90 or 365; null = not logged in as a business
export async function getPanelStats(token: string, days: number): Promise<BusinessStats | null> {
    const res = await apiFetch(`/api/panel/stats?days=${days}`, { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente statistikk (${res.status})`);
    }
    return res.json();
}

export async function setWeeklyReport(token: string, on: boolean): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/weekly-report", { method: "PUT", token, body: { on } }));
}

export async function sendProInterest(token: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/pro-interest", { method: "POST", token }));
}
