"use server";

import { redirect } from "next/navigation";
import { createBooking, createInvite, joinWaitlist, setRebookReminder } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

export async function createBookingAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const businessSlug = String(formData.get("businessSlug"));
    // Only a real page address, so the redirects below can never lead to another website
    if (!/^[a-z0-9-]{1,80}$/.test(businessSlug)) {
        return { error: "Noe gikk galt. Last inn siden på nytt." };
    }
    const serviceId = String(formData.get("serviceId"));
    const employeeId = String(formData.get("employeeId"));
    const startUtc = String(formData.get("startUtc"));
    const date = String(formData.get("date"));
    const units = Number(formData.get("units") ?? 1);
    const token = await getToken();
    if (!token) {
        // Come back to the same time after logging in
        const back = `/${businessSlug}/bestill?${new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, dato: date, tid: startUtc, antall: String(units) })}`;
        redirect(`/logg-inn?neste=${encodeURIComponent(back)}`);
    }
    // Name and email come from the account in the API
    const result = await createBooking({
        serviceId,
        employeeId,
        startUtc,
        customerName: "",
        customerPhone: String(formData.get("customerPhone") ?? ""),
        customerEmail: "",
        // No newsletter yet: nobody sends one, so nobody is asked (see Personvern)
        marketingConsent: false,
        note: String(formData.get("note") ?? ""),
        units,
    }, token);
    const basePath = `/${businessSlug}/bestill`;

    if (!result.ok) {
        return {
            error: result.message,
            values: {
                customerPhone: String(formData.get("customerPhone") ?? ""),
                note: String(formData.get("note") ?? ""),
            },
        };
    }

    const params = new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, tid: startUtc });
    redirect(`${basePath}/bekreftet?${params}`);
}

// BESTI sammen: the invitation is made, then the inviter shares the link from /invitasjon/{token}
export async function createInviteAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const businessSlug = String(formData.get("businessSlug"));
    if (!/^[a-z0-9-]{1,80}$/.test(businessSlug)) {
        return { error: "Noe gikk galt. Last inn siden på nytt." };
    }
    const serviceId = String(formData.get("serviceId"));
    const employeeId = String(formData.get("employeeId") ?? "");
    const startUtc = String(formData.get("startUtc"));
    const date = String(formData.get("date"));
    const units = Number(formData.get("units") ?? 1);
    const persons = Number(formData.get("persons") ?? 2);
    const organizerJoins = formData.get("organizerJoins") !== "0";
    const values = {
        phone: String(formData.get("phone") ?? ""),
        note: String(formData.get("note") ?? ""),
        payMode: String(formData.get("payMode") ?? "HverSelv"),
        decisionMinutes: String(formData.get("decisionMinutes") ?? "30"),
    };
    const token = await getToken();
    if (!token) {
        const back = `/${businessSlug}/bestill?${new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, dato: date, tid: startUtc, antall: String(units), sammen: String(persons), ...(organizerJoins ? {} : { selv: "0" }) })}`;
        redirect(`/logg-inn?neste=${encodeURIComponent(back)}`);
    }
    const result = await createInvite({
        serviceId,
        employeeId: employeeId || null,
        startUtc,
        phone: values.phone,
        payMode: values.payMode === "JegSpanderer" ? "JegSpanderer" : "HverSelv",
        decisionMinutes: Number(values.decisionMinutes),
        note: values.note,
        units,
        persons,
        organizerJoins,
    }, token);
    if (!result.ok) {
        return { error: result.message, values };
    }
    redirect(`/invitasjon/${result.token}?ny=1`);
}

// "Si fra hvis det blir ledig": saved = show "Du står på ventelisten"
export type WaitlistFormState = FormState & { saved?: boolean };

export async function joinWaitlistAction(_previous: WaitlistFormState, formData: FormData): Promise<WaitlistFormState> {
    const serviceId = String(formData.get("serviceId") ?? "");
    const employeeId = String(formData.get("employeeId") ?? "");
    const date = String(formData.get("date") ?? "");
    const token = await getToken();
    if (!token) {
        return { error: "Logg inn for å stå på venteliste." };
    }
    const result = await joinWaitlist(token, { serviceId, employeeId: employeeId === "" ? null : employeeId, date });
    return result.ok ? { saved: true, values: { date } } : { error: result.message, values: { date } };
}

// "Tid for ny time?" on the confirmation page: saved = the chosen number of weeks (0 = Nei takk)
export type RebookFormState = FormState & { weeks?: number };

export async function setRebookReminderAction(_previous: RebookFormState, formData: FormData): Promise<RebookFormState> {
    const token = await getToken();
    if (!token) {
        return { error: "Logg inn igjen for å lagre påminnelsen." };
    }
    const weeks = Number(formData.get("weeks") ?? 0);
    const employeeId = String(formData.get("employeeId") ?? "");
    const result = await setRebookReminder(token, {
        serviceId: String(formData.get("serviceId") ?? ""),
        employeeId: employeeId === "" ? null : employeeId,
        weeks,
    });
    return result.ok ? { weeks } : { error: result.message };
}
