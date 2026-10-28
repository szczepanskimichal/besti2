"use server";
import { redirect } from "next/navigation";
import { addService, addServices, deleteService, getServiceTemplates, updateService } from "@/lib/api";
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
// "Forslag for din bransje": name, price type and description come from the API's own list,
// only the ticked boxes, the length and the price come from the form
export async function addSuggestionsAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const templates = await getServiceTemplates(token);
    const count = Math.min(Number(formData.get("antall")) || 0, templates.length);
    const values: Record<string, string> = {};
    const chosen = [];
    for (let i = 0; i < count; i++) {
        for (const field of [`valgt-${i}`, `tid-${i}`, `pris-${i}`]) {
            values[field] = String(formData.get(field) ?? "");
        }
        const template = templates.find((t) => t.name === formData.get(`navn-${i}`));
        if (!template || formData.get(`valgt-${i}`) !== "on") {
            continue;
        }
        const price = String(formData.get(`pris-${i}`) ?? "").trim();
        chosen.push({
            name: template.name,
            description: template.description,
            durationMinutes: Number(formData.get(`tid-${i}`)),
            priceNok: template.priceType === "EtterAvtale" || price === "" ? null : Number(price),
            priceType: template.priceType,
        });
    }
    if (chosen.length === 0) {
        return { error: "Kryss av minst én tjeneste.", values };
    }
    const result = await addServices(token, chosen);
    if (!result.ok) {
        return { error: result.message, values };
    }
    redirect("/panel/tjenester?forslag=1");
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