"use server";

import { redirect } from "next/navigation";
import { sendProInterest, setWeeklyReport } from "@/lib/api";
import { getToken } from "@/lib/auth";

// The chosen period stays when the page comes back
function back(formData: FormData, flag: string) {
    const days = String(formData.get("dager") ?? "30");
    return `/panel/statistikk?${new URLSearchParams({ dager: /^\d{1,3}$/.test(days) ? days : "30", [flag]: "1" })}`;
}

export async function weeklyReportAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await setWeeklyReport(token, formData.get("on") === "1");
    redirect(back(formData, result.ok ? "lagret" : "feil"));
}

export async function proInterestAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await sendProInterest(token);
    redirect(back(formData, result.ok ? "pro" : "feil"));
}
