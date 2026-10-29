import type {BusinessReviews} from "@/lib/types";
import {API_URL, UPLOAD_TIMEOUT_MS, apiFetch, safeFetch, toResult, type ApiResult} from "./client";

// Reviews: write, show on the business page, reply in the panel

// Customer reviews a past visit; returns the API's message when it fails
export async function createReview(
    token: string,
    input: { bookingId: string; stars: number; comment: string },
): Promise<ApiResult> {
    const res = await apiFetch("/api/me/reviews", { method: "POST", token, body: input });
    return toResult(res, "Kunne ikke lagre vurderingen. Prøv igjen.");
}

// A photo from the visit on the customer's own review (after the review is saved)
export async function uploadReviewPhoto(token: string, bookingId: string, file: File): Promise<ApiResult> {
    const body = new FormData();
    body.append("file", file);
    // Not apiFetch: FormData sets its own Content-Type (with the file boundary)
    const res = await safeFetch(`${API_URL}/api/me/reviews/${encodeURIComponent(bookingId)}/photo`, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body,
        cache: "no-store",
        signal: AbortSignal.timeout(UPLOAD_TIMEOUT_MS),
    });
    return toResult(res);
}

// "Nyttig" on and off; null = not possible (not logged in as a customer, own review)
export async function toggleHelpful(token: string, reviewId: string): Promise<{ marked: boolean; helpful: number } | null> {
    const res = await apiFetch(`/api/reviews/${encodeURIComponent(reviewId)}/helpful`, { method: "POST", token });
    return res.ok ? res.json() : null;
}

const NO_REVIEWS: BusinessReviews = {average: null, count: 0, reviews: []};

// Public: reviews of a business with the average (with a token: which ones the reader marked "Nyttig")
export async function getBusinessReviews(businessId: string, token?: string): Promise<BusinessReviews> {
    // No reviews section rather than a broken business page when the API fails or is too slow
    try {
        const res = await apiFetch(`/api/businesses/${businessId}/reviews`, { token });
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
