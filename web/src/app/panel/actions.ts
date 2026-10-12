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
    const ok = await changeBookingStatus(token, bookingId, action);

    redirect(ok ? "/panel" : "/panel?feil=1");
}

export async function confirmBookingAction(formData: FormData) {
    await changeStatus(formData, "bekreft");
}

export async function cancelBookingAction(formData: FormData) {
    await changeStatus(formData, "avlys");
}
