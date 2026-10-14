"use server";

import { redirect } from "next/navigation";
import { addService, deleteService, updateService } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function addServiceAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const price = String(formData.get("priceNok") ?? "").trim();
    const description = String(formData.get("description") ?? "").trim();

    const result = await addService(token, {
        name: String(formData.get("name") ?? ""),
        description: description === "" ? null : description,
        durationMinutes: Number(formData.get("durationMinutes")),
        priceNok: price === "" ? null : Number(price),
        priceType: String(formData.get("priceType") ?? ""),
    });

    if (!result.ok) {
        redirect(`/panel/tjenester?feil=${encodeURIComponent(result.message)}`);
    }

    redirect("/panel/tjenester?lagt-til=1");
}

    export async function updateServiceAction(formData: FormData) {
        const token = await getToken();
        if (!token) {
            redirect("/logg-inn");
        }
    
        const serviceId = String(formData.get("serviceId") ?? "");
        const price = String(formData.get("priceNok") ?? "").trim();
        const description = String(formData.get("description") ?? "").trim();
    
        const result = await updateService(token, serviceId, {
            name: String(formData.get("name") ?? ""),
            description: description === "" ? null : description,
            durationMinutes: Number(formData.get("durationMinutes")),
            priceNok: price === "" ? null : Number(price),
            priceType: String(formData.get("priceType") ?? ""),
        });
    
        if (!result.ok) {
            redirect(`/panel/tjenester/${serviceId}?feil=${encodeURIComponent(result.message)}`);
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
    
        if (!deleted) {
            redirect("/panel/tjenester?feil=Kunne%20ikke%20slette%20tjenesten.");
        }
    
        redirect("/panel/tjenester?slettet=1");
}