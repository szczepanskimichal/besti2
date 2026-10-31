"use server";

import { redirect } from "next/navigation";
import { replyToReview } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function replyToReviewAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await replyToReview(
        token,
        String(formData.get("reviewId") ?? ""),
        String(formData.get("reply") ?? ""),
    );
    // Stay on the full list when the reply was written there
    const all = formData.get("alle") === "1" ? "&alle=1" : "";
    redirect(result.ok ? `/panel/vurderinger?svart=1${all}` : `/panel/vurderinger?feil=1${all}`);
}