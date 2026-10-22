"use server";
import { redirect } from "next/navigation";
import { deleteNotification, markNotificationsRead } from "@/lib/api";
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