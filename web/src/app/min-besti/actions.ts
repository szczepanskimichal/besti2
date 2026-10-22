"use server";
import { redirect } from "next/navigation";
import { cancelMyBooking, changeMyBookingTime, createReview } from "@/lib/api";
import { getToken } from "@/lib/auth";
export async function cancelMyBookingAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const bookingId = String(formData.get("bookingId") ?? "");
    const result = await cancelMyBooking(token, bookingId);
    redirect(result.ok
        ? "/min-besti?avbestilt=1"
        : "/min-besti?feil=1");
}

// "Endre tid": back to Min Besti when it worked; otherwise back to the same day with a fixed message
// (the time was taken in the meantime, or the visit can no longer be changed)
export async function changeMyBookingTimeAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti");
    }
    const bookingId = String(formData.get("bookingId") ?? "");
    const employeeId = String(formData.get("employeeId") ?? "");
    const date = String(formData.get("date") ?? "");
    const result = await changeMyBookingTime(token, bookingId, {
        employeeId,
        startUtc: String(formData.get("startUtc") ?? ""),
    });
    redirect(result.ok
        ? "/min-besti?endret=1"
        : `/min-besti/endre/${encodeURIComponent(bookingId)}?ansatt=${encodeURIComponent(employeeId)}&dato=${encodeURIComponent(date)}&feil=1`);
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
