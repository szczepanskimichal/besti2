"use server";

import { redirect } from "next/navigation";
import { addService } from "@/lib/api";
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