"use server";
import { redirect } from "next/navigation";
import { addService, deleteService, updateService } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// Errors come back to the form with the typed values (no text in the URL)
export async function addServiceAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await addService(token, serviceInput(formData));
    if (!result.ok) {
        return { error: result.message, values: typedValues(formData) };
    }
    redirect("/panel/tjenester?lagt-til=1");
}
export async function updateServiceAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const serviceId = String(formData.get("serviceId") ?? "");
    const result = await updateService(token, serviceId, serviceInput(formData));
    if (!result.ok) {
        return { error: result.message, values: typedValues(formData) };
    }
    redirect("/panel/tjenester?endret=1");
}
export async function deleteServiceAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const serviceId = String(formData.get("serviceId") ?? "");
    const deleted = await deleteService(token, serviceId);
// Only a short code in the URL; the page shows a fixed text for it
    redirect(deleted ? "/panel/tjenester?slettet=1" : "/panel/tjenester?feil=slett");
}
// The form fields as the API wants them (empty price = "pris etter avtale")
function serviceInput(formData: FormData) {
    const price = String(formData.get("priceNok") ?? "").trim();
    const description = String(formData.get("description") ?? "").trim();
    return {
        name: String(formData.get("name") ?? ""),
        description: description === "" ? null : description,
        durationMinutes: Number(formData.get("durationMinutes")),
        priceNok: price === "" ? null : Number(price),
        priceType: String(formData.get("priceType") ?? ""),
    };
}
// What the owner typed, so the form can show it again after an error
function typedValues(formData: FormData): Record<string, string> {
    const values: Record<string, string> = {};
    for (const field of ["name", "description", "durationMinutes", "priceType", "priceNok"]) {
        values[field] = String(formData.get(field) ?? "");
    }
    return values;
}