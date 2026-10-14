"use server";

import { redirect } from "next/navigation";
import { updateProfile } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function updateProfileAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const description = String(formData.get("description") ?? "").trim();

    const result = await updateProfile(token, {
        description: description === "" ? null : description,
        phone: String(formData.get("phone") ?? ""),
        address: String(formData.get("address") ?? ""),
        postalCode: String(formData.get("postalCode") ?? ""),
        city: String(formData.get("city") ?? ""),
    });

    if (!result.ok) {
        redirect(`/panel/profil?feil=${encodeURIComponent(result.message)}`);
    }

    redirect("/panel/profil?lagret=1");
}