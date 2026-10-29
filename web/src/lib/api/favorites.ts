import type {Favorite} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";
// "Favoritter": the heart on the business page and the list in Min Besti

// null = not logged in
export async function getMyFavorites(token: string): Promise<Favorite[] | null> {
    const res = await apiFetch("/api/me/favorites", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente favoritter (${res.status})`);
    }
    return res.json();
}

// The business page only needs to know if its heart is filled; an empty list rather than a broken page
export async function getMyFavoriteIds(token: string): Promise<string[]> {
    const res = await apiFetch("/api/me/favorites/ids", { token });
    return res.ok ? res.json() : [];
}

export async function setFavorite(token: string, businessId: string, on: boolean): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/me/favorites/${encodeURIComponent(businessId)}`,
        { method: on ? "PUT" : "DELETE", token }));
}
