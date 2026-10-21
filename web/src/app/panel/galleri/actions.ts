"use server";

import { redirect } from "next/navigation";
import { deleteGalleryImage, uploadGalleryImage } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// Errors come back to the form (no text in the URL); success reloads the page with a short code
export async function uploadGalleryImageAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const file = formData.get("image");
    if (!(file instanceof File) || file.size === 0) {
        return { error: "Velg et bilde." };
    }
    const result = await uploadGalleryImage(token, file);
    if (!result.ok) {
        return { error: result.message };
    }
    redirect("/panel/galleri?lagt=1");
}

export async function deleteGalleryImageAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const ok = await deleteGalleryImage(token, String(formData.get("imageId") ?? ""));
    if (!ok) {
        return { error: "Bildet kunne ikke slettes. Prøv igjen." };
    }
    redirect("/panel/galleri?slettet=1");
}
