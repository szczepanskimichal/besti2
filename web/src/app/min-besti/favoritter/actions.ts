"use server";

import { redirect } from "next/navigation";
import { setFavorite } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";

// The heart on the business page and "Fjern" in Favoritter; back to the page it was pressed on
export async function toggleFavoriteAction(formData: FormData) {
    const back = safeRedirectPath(String(formData.get("tilbake") ?? "/min-besti/favoritter"));
    const token = await getToken();
    if (!token) {
        redirect(`/logg-inn?neste=${encodeURIComponent(back)}`);
    }
    const result = await setFavorite(token, String(formData.get("businessId") ?? ""), formData.get("on") === "1");
    redirect(result.ok ? back : `${back}${back.includes("?") ? "&" : "?"}favoritt=feil`);
}
