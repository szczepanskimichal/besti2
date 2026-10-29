import type {LoyaltyCard, MyYear, PanelLoyalty} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";
// "Stempelkort" (panel and Min Besti) and "Mitt BESTI-år"

// null = not logged in as a business
export async function getPanelLoyalty(token: string): Promise<PanelLoyalty | null> {
    const res = await apiFetch("/api/panel/loyalty", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente stempelkortet (${res.status})`);
    }
    return res.json();
}

// visits null = turn the card off
export async function setLoyalty(token: string, visits: number | null, reward: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/loyalty", { method: "PUT", token, body: { visits, reward } }));
}

export async function redeemLoyalty(token: string, customerUserId: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/panel/loyalty/redeem", { method: "POST", token, body: { customerUserId } }));
}

// The customer's cards; an empty list rather than a broken page
export async function getMyLoyalty(token: string): Promise<LoyaltyCard[]> {
    const res = await apiFetch("/api/me/loyalty", { token });
    return res.ok ? res.json() : [];
}

// null = not logged in
export async function getMyYear(token: string, year?: number): Promise<MyYear | null> {
    const res = await apiFetch(`/api/me/year${year ? `?year=${year}` : ""}`, { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente året (${res.status})`);
    }
    return res.json();
}
