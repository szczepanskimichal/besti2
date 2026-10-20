"use server";

import { redirect } from "next/navigation";
import { deleteGalleryImage, uploadGalleryImage } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function uploadGalleryImageAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const file = formData.get("image");
    if (!(file instanceof File) || file.size === 0) {
        redirect("/panel/galleri?feil=1");
    }
    const result = await uploadGalleryImage(token, file);
    redirect(result.ok ? "/panel/galleri?lagt=1" : `/panel/galleri?feil=${encodeURIComponent(result.message)}`);
}

export async function deleteGalleryImageAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const ok = await deleteGalleryImage(token, String(formData.get("imageId") ?? ""));
    redirect(ok ? "/panel/galleri?slettet=1" : "/panel/galleri?feil=1");
}