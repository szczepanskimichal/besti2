import type {NotificationList} from "@/lib/types";
import {API_URL, apiFetch, toResult, type ApiResult} from "./client";

// The bell (🔔); null = not logged in or the API failed (the header must never break because of it)
export async function getNotifications(token: string): Promise<NotificationList | null> {
    const res = await apiFetch("/api/notifications", { token });
    return res.ok ? res.json() : null;
}

// "Merk alle som lest"
export async function markNotificationsRead(token: string): Promise<void> {
    await apiFetch("/api/notifications/read", { method: "POST", token });
}

// A click on one notification: marks it as read and returns where it leads (null = not found)
export async function markNotificationRead(token: string, id: string): Promise<string | null> {
    const res = await apiFetch(`/api/notifications/${id}/read`, { method: "POST", token });
    if (!res.ok) {
        return null;
    }
    const body = await res.json();
    return body.link ?? "/varsler";
}

export async function deleteNotification(token: string, id: string): Promise<void> {
    await apiFetch(`/api/notifications/${id}`, { method: "DELETE", token });
}

// The live bell: an open connection from the API (Server-Sent Events). Not apiFetch: it must stay open,
// so no 8 s timeout; it closes when the browser leaves (signal)
export async function openNotificationStream(token: string, signal: AbortSignal): Promise<Response> {
    return fetch(`${API_URL}/api/notifications/stream`, {
        headers: {Authorization: `Bearer ${token}`},
        cache: "no-store",
        signal,
    });
}

// Only the number on the bell (the header on every page); null = the API failed
export async function getUnreadCount(token: string): Promise<number | null> {
    const res = await apiFetch("/api/notifications/unread", { token });
    return res.ok ? (await res.json()).unread : null;
}

// "Varsler på telefonen": the server's public VAPID key; null = push is not set up on this server
export async function getPushPublicKey(): Promise<string | null> {
    const res = await apiFetch("/api/push/public-key");
    return res.ok ? (await res.json()).publicKey ?? null : null;
}

// This device gets push messages for the logged-in person (and the business, for an owner)
export async function savePushSubscription(token: string, input: { endpoint: string; p256dh: string; auth: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/push/subscriptions", { method: "POST", token, body: input }));
}

export async function removePushSubscription(token: string, endpoint: string): Promise<void> {
    await apiFetch("/api/push/subscriptions/remove", { method: "POST", token, body: { endpoint } });
}
