"use server";
import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { acceptInvite, declineInvite, deleteInviteMessage, reactToInviteMessage, sendInviteMessage, withdrawInvite } from "@/lib/api";
import type { ApiResult } from "@/lib/api/client";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// The link token is URL-safe base64 (SecureToken in the API); anything else is a broken link
function inviteToken(formData: FormData): string | null {
    const token = String(formData.get("token") ?? "");
    return /^[A-Za-z0-9_-]{20,100}$/.test(token) ? token : null;
}

// "Skriv en liten hilsen" travels with the answer (empty = no greeting)
function greeting(formData: FormData) {
    return {
        greeting: String(formData.get("friendMessage") ?? "").trim(),
        greetingIcon: String(formData.get("friendMessageIcon") ?? ""),
    };
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
    const result = await acceptInvite(token, auth, phone, note, greeting(formData));
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
    const result = await declineInvite(token, await getToken(), greeting(formData));
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

// BESTI-chat: called from the chat itself; the page is drawn again with the new line
const CHAT_ERROR = "Meldingen kunne ikke sendes. Prøv igjen.";

async function chatCall(inviteToken: string, call: (auth: string, token: string) => Promise<ApiResult>): Promise<ApiResult> {
    const token = /^[A-Za-z0-9_-]{20,100}$/.test(inviteToken) ? inviteToken : null;
    const auth = await getToken();
    if (!token || !auth) {
        return { ok: false, message: CHAT_ERROR };
    }
    const result = await call(auth, token);
    revalidatePath(`/invitasjon/${token}`);
    return result;
}

export async function sendChatMessageAction(inviteToken: string, text: string): Promise<ApiResult> {
    return chatCall(inviteToken, (auth, token) => sendInviteMessage(token, auth, text));
}

export async function deleteChatMessageAction(inviteToken: string, messageId: string): Promise<ApiResult> {
    return chatCall(inviteToken, (auth, token) => deleteInviteMessage(token, auth, messageId));
}

export async function reactChatMessageAction(inviteToken: string, messageId: string, kind: string): Promise<ApiResult> {
    return chatCall(inviteToken, (auth, token) => reactToInviteMessage(token, auth, messageId, kind));
}
