"use server";
import { redirect } from "next/navigation";
import { acceptStaffInvite, leaveStaffPlace } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// "Godta" on /ansatt/invitasjon: connects this account to the employee, then shows the plan
export async function acceptStaffInviteAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const code = String(formData.get("kode") ?? "");
    const token = await getToken();
    if (!token) {
        redirect(`/logg-inn?neste=${encodeURIComponent(`/ansatt/invitasjon?kode=${code}`)}`);
    }
    const result = await acceptStaffInvite(token, code);
    if (!result.ok) {
        return { error: result.message };
    }
    redirect("/ansatt?velkommen=1");
}

// "Slutt å se planen": the employee no longer sees this business's visits
export async function leaveStaffPlaceAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/ansatt");
    }
    await leaveStaffPlace(token, String(formData.get("employeeId") ?? ""));
    redirect("/ansatt");
}
