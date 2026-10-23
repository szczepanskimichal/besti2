
"use server";
import { redirect } from "next/navigation";
import { approveBusiness } from "@/lib/api";

// The token is used once, so the result page does not need it any more
export async function approveBusinessAction(formData: FormData) {
    const result = await approveBusiness(String(formData.get("token") ?? ""));
    redirect(result.ok ? "/godkjenn?svar=godkjent" : "/godkjenn?svar=feil");
}