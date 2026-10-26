"use server";

import { redirect } from "next/navigation";
import { updateProfile, uploadLogo } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// Errors come back to the form with the typed values (no text in the URL)
export async function updateProfileAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const field of ["description", "phone", "contactEmail", "address", "postalCode", "city", "freeCancelHours"]) {
        values[field] = String(formData.get(field) ?? "");
    }

    const description = values.description.trim();
    const result = await updateProfile(token, {
        description: description === "" ? null : description,
        phone: values.phone,
        contactEmail: values.contactEmail.trim() === "" ? null : values.contactEmail,
        address: values.address,
        postalCode: values.postalCode,
        city: values.city,
        freeCancelHours: Number(values.freeCancelHours || 24),
    });

    if (!result.ok) {
        return { error: result.message, values };
    }

    redirect("/panel/profil?lagret=1");
}

export async function uploadLogoAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const file = formData.get("logo");
    if (!(file instanceof File) || file.size === 0) {
        return { error: "Velg et bilde." };
    }
    const result = await uploadLogo(token, file);
    if (!result.ok) {
        return { error: result.message };
    }
    redirect("/panel/profil?logo=1");
}
