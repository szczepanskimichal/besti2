"use server";
import { redirect } from "next/navigation";
import { answerMyNewTime, cancelMyBooking, createReview } from "@/lib/api";
import { getToken } from "@/lib/auth";
export async function cancelMyBookingAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await cancelMyBooking(token, bookingId, String(formData.get("melding") ?? ""));
    redirect(result.ok
        ? "/min-besti?avbestilt=1"
        : "/min-besti?feil=1");
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
export async function createReviewAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/vurderinger");
    }
    const result = await createReview(token, {
        bookingId: String(formData.get("bookingId") ?? ""),
        stars: Number(formData.get("stars")),
        comment: String(formData.get("comment") ?? ""),
    });
    redirect(result.ok
        ? "/min-besti/vurderinger?takk=1"
        : "/min-besti/vurderinger?feil=1");
}
