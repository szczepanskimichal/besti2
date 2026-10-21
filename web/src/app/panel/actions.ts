"use server";

import { redirect } from "next/navigation";
import { changeBookingStatus, createManualBooking } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";
import type { FormState } from "@/lib/formState";


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
const MANUAL_FIELDS = ["serviceId", "employeeId", "date", "time", "customerName", "customerPhone", "customerEmail",
    "note", "sendConfirmation"];

// "+ Ny bestilling": errors come back to the form with everything typed; on success the calendar
// shows the day of the new visit
export async function createManualBookingAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const field of MANUAL_FIELDS) {
        values[field] = String(formData.get(field) ?? "");
    }

    const result = await createManualBooking(token, {
        serviceId: values.serviceId,
        employeeId: values.employeeId,
        date: values.date,
        time: values.time,
        customerName: values.customerName,
        customerPhone: values.customerPhone,
        customerEmail: values.customerEmail.trim() === "" ? null : values.customerEmail,
        note: values.note,
        sendConfirmation: values.sendConfirmation === "on",
    });
    if (!result.ok) {
        return { error: result.message, values };
    }

    const week = formData.get("visning") === "uke" ? "&visning=uke" : "";
    redirect(`/panel?dag=${values.date}${week}`);
}

