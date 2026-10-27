import type {BusinessReviews} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";

// Reviews: write, show on the business page, reply in the panel

// Customer reviews a past visit; returns the API's message when it fails
export async function createReview(
    token: string,
    input: { bookingId: string; stars: number; comment: string },
): Promise<ApiResult> {
    const res = await apiFetch("/api/me/reviews", { method: "POST", token, body: input });
    return toResult(res, "Kunne ikke lagre vurderingen. Prøv igjen.");
}

const NO_REVIEWS: BusinessReviews = {average: null, count: 0, reviews: []};

// Public: reviews of a business with the average
export async function getBusinessReviews(businessId: string): Promise<BusinessReviews> {
    // No reviews section rather than a broken business page when the API fails or is too slow
    try {
        const res = await apiFetch(`/api/businesses/${businessId}/reviews`);
        return res.ok ? await res.json() : NO_REVIEWS;
    } catch {
        return NO_REVIEWS;
    }
}

// Business panel: the owner's reviews (null = not logged in as a business)
export async function getPanelReviews(token: string): Promise<BusinessReviews | null> {
    const res = await apiFetch("/api/panel/reviews", {token});
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente vurderinger (${res.status})`);
    }
    return res.json();
}

export async function replyToReview(token: string, reviewId: string, reply: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/panel/reviews/${reviewId}/reply`, {method: "POST", token, body: {reply}}));
}
