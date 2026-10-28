"use server";
import { redirect } from "next/navigation";
import { acceptInvite, declineInvite, withdrawInvite } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// The link token is URL-safe base64 (SecureToken in the API); anything else is a broken link
function inviteToken(formData: FormData): string | null {
    const token = String(formData.get("token") ?? "");
    return /^[A-Za-z0-9_-]{20,100}$/.test(token) ? token : null;
}

// "Jeg blir med!": both visits are booked, then the page shows the celebration
export async function acceptInviteAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = inviteToken(formData);
    if (!token) {
        return { error: "Lenken er ikke gyldig." };
    }
    const here = `/invitasjon/${token}`;
    const auth = await getToken();
    if (!auth) {
        redirect(`/logg-inn?neste=${encodeURIComponent(here)}`);
    }
    const phone = String(formData.get("phone") ?? "");
    const note = String(formData.get("note") ?? "");
    const result = await acceptInvite(token, auth, phone, note);
    if (!result.ok) {
        return { error: result.message, values: { phone, note } };
    }
    redirect(`${here}?svar=ja`);
}

// "Nei takk" (no login needed)
export async function declineInviteAction(formData: FormData) {
    const token = inviteToken(formData);
    if (!token) {
        redirect("/");
    }
    const result = await declineInvite(token, await getToken());
    redirect(`/invitasjon/${token}?svar=${result.ok ? "nei" : "feil"}`);
}

// The inviter takes it back (from the invitation page or Min Besti)
export async function withdrawInviteAction(formData: FormData) {
    const auth = await getToken();
    if (!auth) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const inviteId = String(formData.get("inviteId") ?? "");
    const result = await withdrawInvite(auth, inviteId);
    const token = inviteToken(formData);
    if (token) {
        redirect(`/invitasjon/${token}${result.ok ? "" : "?svar=feil"}`);
    }
    redirect(result.ok ? "/min-besti?invitasjon=trukket" : "/min-besti?feil=invitasjon");
}
