"use server";

import { redirect } from "next/navigation";
import { changeBookingStatus } from "@/lib/api";
import { getToken } from "@/lib/auth";

async function changeStatus(formData: FormData, action: "bekreft" | "avlys") {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await changeBookingStatus(token, bookingId, action);

    // Only a short code in the URL; the page shows a fixed text for it
    redirect(result === "ok" ? "/panel" : result === "tooLate" ? "/panel?feil=for-sent" : "/panel?feil=1");
}

export async function confirmBookingAction(formData: FormData) {
    await changeStatus(formData, "bekreft");
}

export async function cancelBookingAction(formData: FormData) {
    await changeStatus(formData, "avlys");
}
