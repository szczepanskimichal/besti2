
"use server";
import { redirect } from "next/navigation";
import { approveBusiness, rejectBusiness } from "@/lib/api";

// The token is used once, so the result page does not need it any more
export async function approveBusinessAction(formData: FormData) {
    const result = await approveBusiness(String(formData.get("token") ?? ""));
    redirect(result.ok ? "/godkjenn?svar=godkjent" : "/godkjenn?svar=feil");
}
// "Avvis og slett": deletes the business and the owner account; the checkbox must be ticked
export async function rejectBusinessAction(formData: FormData) {
    if (formData.get("bekreft") !== "ja") {
        redirect("/godkjenn?svar=feil");
    }
    const result = await rejectBusiness(String(formData.get("token") ?? ""));
    redirect(result.ok ? "/godkjenn?svar=avvist" : "/godkjenn?svar=feil");
}