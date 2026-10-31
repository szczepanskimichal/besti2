import type {CreateInviteInput, CustomerInvite, Invite} from "@/lib/types";
import {apiFetch, toResult, type ApiResult} from "./client";

// BESTI sammen: send, look at, answer and manage invitations

export type InviteLinkResult = { ok: true; token: string } | { ok: false; message: string };

// The link token comes back only here (and from "Del igjen"): it is the secret part of /invitasjon/{token}
async function toLink(res: Response): Promise<InviteLinkResult> {
    if (res.ok) {
        const body = await res.json();
        return { ok: true, token: body.token };
    }
    const body = await res.json().catch(() => null);
    return { ok: false, message: body?.message ?? "Noe gikk galt. Prøv igjen." };
}

export async function createInvite(input: CreateInviteInput, token: string): Promise<InviteLinkResult> {
    return toLink(await apiFetch("/api/invites", { method: "POST", token, body: input, forwardIp: true }));
}

// Open to everyone with the link; with a login the API also says if the visitor is the inviter (null = wrong link)
export async function getInvite(inviteToken: string, token?: string): Promise<Invite | null> {
    const res = await apiFetch(`/api/invites/${encodeURIComponent(inviteToken)}`, { token, forwardIp: true });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente invitasjonen (${res.status})`);
    }
    return res.json();
}

// A short greeting to the inviter with the answer ("Skriv en liten hilsen"); icon = the crystal of a ready line
export type Greeting = { greeting: string; greetingIcon: string };

// "Jeg blir med!": both visits are booked now
export async function acceptInvite(inviteToken: string, token: string, phone: string, note: string,
                                   greeting: Greeting): Promise<ApiResult> {
    const res = await apiFetch(`/api/invites/${encodeURIComponent(inviteToken)}/accept`,
        { method: "POST", token, body: { phone, note, ...greeting }, forwardIp: true });
    return toResult(res);
}

// "Nei takk" also works without login
export async function declineInvite(inviteToken: string, token: string | undefined, greeting: Greeting): Promise<ApiResult> {
    const res = await apiFetch(`/api/invites/${encodeURIComponent(inviteToken)}/decline`,
        { method: "POST", token, body: greeting, forwardIp: true });
    return toResult(res);
}

// BESTI-chat: write, delete, and a crystal reaction (the same kind again takes it back)
export async function sendInviteMessage(inviteToken: string, token: string, text: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/invites/${encodeURIComponent(inviteToken)}/messages`,
        { method: "POST", token, body: { text } }));
}

export async function deleteInviteMessage(inviteToken: string, token: string, messageId: string): Promise<ApiResult> {
    return toResult(await apiFetch(
        `/api/invites/${encodeURIComponent(inviteToken)}/messages/${encodeURIComponent(messageId)}`,
        { method: "DELETE", token }));
}

export async function reactToInviteMessage(inviteToken: string, token: string, messageId: string,
                                           kind: string): Promise<ApiResult> {
    return toResult(await apiFetch(
        `/api/invites/${encodeURIComponent(inviteToken)}/messages/${encodeURIComponent(messageId)}/reactions`,
        { method: "POST", token, body: { kind } }));
}

// Min Besti: invitations I sent or answered; null = not logged in
export async function getMyInvites(token: string): Promise<CustomerInvite[] | null> {
    const res = await apiFetch("/api/me/invites", { token });
    if (res.status === 401 || res.status === 403) {
        return null;
    }
    if (!res.ok) {
        throw new Error(`Kunne ikke hente invitasjoner (${res.status})`);
    }
    return res.json();
}

export async function withdrawInvite(token: string, inviteId: string): Promise<ApiResult> {
    return toResult(await apiFetch(`/api/me/invites/${inviteId}/trekk`, { method: "POST", token }));
}

// "Del igjen": a new link (the old one stops working)
export async function newInviteLink(token: string, inviteId: string): Promise<InviteLinkResult> {
    return toLink(await apiFetch(`/api/me/invites/${inviteId}/lenke`, { method: "POST", token }));
}
