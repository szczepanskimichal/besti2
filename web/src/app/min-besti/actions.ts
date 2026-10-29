"use server";
import { redirect } from "next/navigation";
import { answerMyNewTime, cancelMyBooking, createReview, leaveWaitlist, newInviteLink, removeRebookReminder, uploadReviewPhoto } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// On an error the API's own message goes back to the form, with the typed reason
export async function cancelMyBookingAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const bookingId = String(formData.get("bookingId") ?? "");
    const melding = String(formData.get("melding") ?? "");
    const result = await cancelMyBooking(token, bookingId, melding);
    if (!result.ok) {
        return { error: result.message, values: { melding } };
    }
    redirect("/min-besti?avbestilt=1");
}

// Godta / Passer ikke for a visit the business moved
export async function answerMyNewTimeAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const accept = formData.get("svar") === "ja";
    const result = await answerMyNewTime(token, String(formData.get("bookingId") ?? ""), accept);
    redirect(result.ok
        ? `/min-besti?svar=${accept ? "godtatt" : "avslatt"}`
        : "/min-besti?feil=ny-tid");
}

// On an error (e.g. "already reviewed") the API's message goes back to the form, with the stars and comment
export async function createReviewAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/vurderinger");
    }
    const stars = String(formData.get("stars") ?? "");
    const comment = String(formData.get("comment") ?? "");
    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await createReview(token, {
        bookingId,
        stars: Number(stars),
        comment,
    });
    if (!result.ok) {
        return { error: result.message, values: { stars, comment } };
    }
    // The photo is extra: when it fails, the review is still saved and the page says so
    const photo = formData.get("photo");
    if (photo instanceof File && photo.size > 0) {
        const uploaded = await uploadReviewPhoto(token, bookingId, photo);
        if (!uploaded.ok) {
            redirect("/min-besti/vurderinger?takk=1&bilde=feil");
        }
    }
    redirect("/min-besti/vurderinger?takk=1");
}

// BESTI sammen "Del igjen": a new link (the old one stops working), then the share page
export async function shareInviteAgainAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const result = await newInviteLink(token, String(formData.get("inviteId") ?? ""));
    redirect(result.ok ? `/invitasjon/${result.token}` : "/min-besti?feil=invitasjon");
}

// "Fjern" on a day in the waitlist
export async function leaveWaitlistAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    await leaveWaitlist(token, String(formData.get("id") ?? ""));
    redirect("/min-besti");
}

// "Slå av" on a "Tid for ny time?" reminder
export async function removeRebookReminderAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    await removeRebookReminder(token, String(formData.get("id") ?? ""));
    redirect("/min-besti");
}
