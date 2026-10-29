"use server";

import { redirect } from "next/navigation";
import { decideBusiness, hideReview, reopenBusiness, resendConfirmation, setAdminNote, showReview, suspendBusiness } from "@/lib/api";
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

// "Steng bedriften" (with a reason) or "Åpne igjen"
export async function suspendAction(formData: FormData) {
    const token = await adminToken();
    const id = String(formData.get("id") ?? "");
    const result = formData.get("open") === "1"
        ? await reopenBusiness(token, id)
        : await suspendBusiness(token, id, String(formData.get("reason") ?? ""));
    redirect(`/admin/bedrifter/${encodeURIComponent(id)}?${result.ok ? "endret=1" : `feil=${encodeURIComponent(result.message)}`}`);
}

// "Skjul" (with a reason) or "Vis igjen" a review; back to the list it came from
export async function reviewAction(formData: FormData) {
    const token = await adminToken();
    const id = String(formData.get("id") ?? "");
    const back = formData.get("back") === "skjulte" ? "/admin/vurderinger?vis=skjulte" : "/admin/vurderinger";
    const result = formData.get("show") === "1"
        ? await showReview(token, id)
        : await hideReview(token, id, String(formData.get("reason") ?? ""));
    const separator = back.includes("?") ? "&" : "?";
    redirect(`${back}${separator}${result.ok ? "endret=1" : `feil=${encodeURIComponent(result.message)}`}#v-${id}`);
}

// "Send ny bekreftelseslenke" to a customer who never got the first e-mail
export async function resendAction(formData: FormData) {
    const token = await adminToken();
    const q = String(formData.get("q") ?? "");
    const result = await resendConfirmation(token, String(formData.get("id") ?? ""));
    redirect(`/admin/kunder?q=${encodeURIComponent(q)}&${result.ok ? "sendt=1" : `feil=${encodeURIComponent(result.message)}`}`);
}
