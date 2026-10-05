"use server";

import { redirect } from "next/navigation";
import { createBooking } from "@/lib/api";

export async function createBookingAction(formData: FormData) {
    const businessId = String(formData.get("businessId"));
    const serviceId = String(formData.get("serviceId"));
    const employeeId = String(formData.get("employeeId"));
    const startUtc = String(formData.get("startUtc"));
    const date = String(formData.get("date"));

    const result = await createBooking({
        serviceId,
        employeeId,
        startUtc,
        customerName: String(formData.get("customerName") ?? ""),
        customerPhone: String(formData.get("customerPhone") ?? ""),
        customerEmail: String(formData.get("customerEmail") ?? ""),
        marketingConsent: formData.get("marketingConsent") === "on",
    });

    const basePath = `/bedrift/${businessId}/bestill`;

    if (!result.ok) {
        const params = new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, dato: date, tid: startUtc, feil: result.message });
        redirect(`${basePath}?${params}`);
    }

    const params = new URLSearchParams({ tjeneste: serviceId, ansatt: employeeId, tid: startUtc });
    redirect(`${basePath}/bekreftet?${params}`);
}
