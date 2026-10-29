"use server";

import { redirect } from "next/navigation";
import { decideBusiness, setAdminNote } from "@/lib/api";
import { getToken } from "@/lib/auth";

async function adminToken() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/admin");
    }
    return token;
}

// "Godkjenn" or (after the confirm step) "Avvis" a business that waits for approval
export async function decideAction(formData: FormData) {
    const token = await adminToken();
    const id = String(formData.get("id") ?? "");
    const approve = formData.get("decision") === "godkjenn";
    const result = await decideBusiness(token, id, approve);
    if (!result.ok) {
        redirect(`/admin?feil=${encodeURIComponent(result.message)}`);
    }
    // A rejected business is deleted: back to the overview
    redirect(approve ? `/admin/bedrifter/${encodeURIComponent(id)}?godkjent=1` : "/admin?avvist=1");
}

export async function noteAction(formData: FormData) {
    const token = await adminToken();
    const id = String(formData.get("id") ?? "");
    const result = await setAdminNote(token, id, String(formData.get("note") ?? ""));
    redirect(`/admin/bedrifter/${encodeURIComponent(id)}?${result.ok ? "lagret=1" : `feil=${encodeURIComponent(result.message)}`}`);
}
