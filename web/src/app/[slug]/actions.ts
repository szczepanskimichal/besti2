"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { toggleHelpful } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "Nyttig" on a review, then back to the same review on the business page
export async function toggleHelpfulAction(formData: FormData) {
    const slug = String(formData.get("slug") ?? "");
    // Only a business page address (letters, digits and dashes), never somewhere else
    const page = /^[a-z0-9-]{1,80}$/.test(slug) ? `/${slug}` : "/";
    const token = await getToken();
    if (!token) {
        redirect(`/logg-inn?neste=${encodeURIComponent(page)}`);
    }
    await toggleHelpful(token, String(formData.get("reviewId") ?? ""));
    // The redirect only changes the #, so without this the browser keeps showing the old count
    revalidatePath(page);
    redirect(`${page}#vurderinger`);
}
