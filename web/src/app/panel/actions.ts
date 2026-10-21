"use server";

import { redirect } from "next/navigation";
import { changeBookingStatus } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";

async function changeStatus(formData: FormData, action: "bekreft" | "avlys") {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await changeBookingStatus(token, bookingId, action);

    // Back to the calendar day or the list the owner came from (only panel pages)
    const back = safeRedirectPath(String(formData.get("tilbake") ?? ""));
    const page = back.startsWith("/panel") ? back : "/panel";
    // Only a short code in the URL; the page shows a fixed text for it
    const code = result === "ok" ? "" : result === "tooLate" ? "for-sent" : "1";
    redirect(code ? `${page}${page.includes("?") ? "&" : "?"}feil=${code}` : page);
}

export async function confirmBookingAction(formData: FormData) {
    await changeStatus(formData, "bekreft");
}

export async function cancelBookingAction(formData: FormData) {
    await changeStatus(formData, "avlys");
}
