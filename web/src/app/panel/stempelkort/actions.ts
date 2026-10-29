"use server";

import { redirect } from "next/navigation";
import { redeemLoyalty, setLoyalty } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function saveLoyaltyAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const off = formData.get("off") === "1";
    const result = await setLoyalty(token, off ? null : Number(formData.get("visits")), String(formData.get("reward") ?? ""));
    redirect(result.ok ? `/panel/stempelkort?${off ? "av" : "lagret"}=1` : `/panel/stempelkort?feil=${encodeURIComponent(result.message)}`);
}

export async function redeemLoyaltyAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await redeemLoyalty(token, String(formData.get("customerUserId") ?? ""));
    redirect(result.ok ? "/panel/stempelkort?gitt=1" : `/panel/stempelkort?feil=${encodeURIComponent(result.message)}`);
}
