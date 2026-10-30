"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { changeBookingStatus, createManualBooking, createTimeOff, deleteTimeOff, moveBooking } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";
import { FIRST_STEPS_COOKIE } from "./firstStepsCookie";
import type { FormState } from "@/lib/formState";


async function changeStatus(formData: FormData, action: "bekreft" | "avlys" | "motte-ikke") {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await changeBookingStatus(token, bookingId, action, String(formData.get("melding") ?? ""));

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
export async function noShowAction(formData: FormData) {
    await changeStatus(formData, "motte-ikke");
}

// Back to the same view (year, month, week or plan) after saving
function viewParam(formData: FormData): string {
    const view = formData.get("visning");
    return view === "ar" || view === "uke" || view === "maned" || view === "plan" ? `&visning=${view}` : "";
}

const MANUAL_FIELDS = ["serviceId", "employeeId", "date", "time", "customerName", "customerPhone", "customerEmail",
    "note", "sendConfirmation", "units"];

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
        units: Number(values.units) || 1, // no field = a service with a fixed length
    });
    if (!result.ok) {
        return { error: result.message, values };
    }
    redirect(`/panel?dag=${values.date}${viewParam(formData)}`);
}
const TIME_OFF_FIELDS = ["employeeId", "fromDate", "fromTime", "toDate", "toTime", "allDay", "reason"];

// "Blokker tid": errors come back to the form; on success the calendar shows the first blocked day,
// with a warning when visits are already in that time
export async function createTimeOffAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const field of TIME_OFF_FIELDS) {
        values[field] = String(formData.get(field) ?? "");
    }
    const allDay = values.allDay === "on";

    const result = await createTimeOff(token, {
        employeeId: values.employeeId || null, // "" = Hele bedriften
        fromDate: values.fromDate,
        fromTime: allDay || !values.fromTime ? "00:00" : values.fromTime,
        toDate: values.toDate,
        toTime: allDay || !values.toTime ? "00:00" : values.toTime,
        allDay,
        reason: values.reason,
    });
    if (!result.ok) {
        return { error: result.message, values };
    }
    const warning = result.overlapping > 0 ? `&varsel=${result.overlapping}` : "";
    redirect(`/panel?dag=${values.fromDate}${viewParam(formData)}${warning}`);
}

export async function deleteTimeOffAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const ok = await deleteTimeOff(token, String(formData.get("timeOffId") ?? ""));

    const back = safeRedirectPath(String(formData.get("tilbake") ?? ""));
    const page = back.startsWith("/panel") ? back : "/panel";
    redirect(ok ? page : `${page}${page.includes("?") ? "&" : "?"}feil=1`);
}
const MOVE_FIELDS = ["employeeId", "date", "time", "svar"];

// "Flytt": errors come back to the form; on success the calendar shows the day of the new time
export async function moveBookingAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const field of MOVE_FIELDS) {
        values[field] = String(formData.get(field) ?? "");
    }

    const result = await moveBooking(token, String(formData.get("bookingId") ?? ""), {
        employeeId: values.employeeId,
        date: values.date,
        time: values.time,
        // "spor" = the customer must accept (link in the e-mail); "ja" = they already agreed, only tell them
        askCustomer: values.svar === "spor",
        notifyCustomer: values.svar === "ja",
    });
    if (!result.ok) {
        return { error: result.message, values };
    }

    redirect(`/panel?dag=${values.date}${viewParam(formData)}`);
}


// "Skjul listen" on Kom i gang: remembered in this browser for a year
export async function hideFirstStepsAction() {
    (await cookies()).set(FIRST_STEPS_COOKIE, "skjult", {
        httpOnly: true,
        sameSite: "lax",
        secure: process.env.NODE_ENV === "production",
        path: "/panel",
        maxAge: 60 * 60 * 24 * 365,
    });
    redirect("/panel");
}
