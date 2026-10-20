"use server";

import { redirect } from "next/navigation";
import { createBooking } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function createBookingAction(formData: FormData) {
    const businessId = String(formData.get("businessId"));
    const serviceId = String(formData.get("serviceId"));
    const employeeId = String(formData.get("employeeId"));
    const startUtc = String(formData.get("startUtc"));
    const date = String(formData.get("date"));

    const token = await getToken();
    if (!token) {
    // Come back to the same time after logging in
        const back = `/bedrift/${businessId}/bestill?${new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, dato: date, tid: startUtc })}`;
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
        marketingConsent: formData.get("marketingConsent") === "on",
        note: String(formData.get("note") ?? ""),
    }, token);
    const basePath = `/bedrift/${businessId}/bestill`;

    if (!result.ok) {
        const params = new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, dato: date, tid: startUtc, feil: result.message });
        redirect(`${basePath}?${params}`);
    }

    const params = new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, tid: startUtc });
    redirect(`${basePath}/bekreftet?${params}`);
}
