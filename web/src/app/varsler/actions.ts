"use server";
import { redirect } from "next/navigation";
import { type ApiResult, deleteNotification, markNotificationsRead, removePushSubscription, savePushSubscription, setNotificationSettings } from "@/lib/api";
import { getToken } from "@/lib/auth";

// The bin (🗑️) next to one notification
export async function deleteNotificationAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/varsler");
    }
    await deleteNotification(token, String(formData.get("id") ?? ""));
    redirect("/varsler");
}

export async function markAllReadAction() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/varsler");
    }
    await markNotificationsRead(token);
    redirect("/varsler");
}
// "Varsler på telefonen": this device gets push messages / stops getting them
export async function savePushAction(input: { endpoint: string; p256dh: string; auth: string }): Promise<ApiResult> {
    const token = await getToken();
    if (!token) {
        return { ok: false, message: "Du er logget ut. Logg inn igjen." };
    }
    return savePushSubscription(token, input);
}

export async function removePushAction(endpoint: string): Promise<void> {
    const token = await getToken();
    if (token) {
        await removePushSubscription(token, endpoint);
    }
}

// "E-post dagen før" / "Snart tid! 2 timer før": each button sends both choices, with its own one flipped
export async function notificationSettingsAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/varsler");
    }
    await setNotificationSettings(token, {
        reminderEmail: formData.get("reminderEmail") === "true",
        soonNotice: formData.get("soonNotice") === "true",
    });
    redirect("/varsler");
}
