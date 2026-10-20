"use server";
import { redirect } from "next/navigation";
import { cancelMyBooking } from "@/lib/api";
import { getToken } from "@/lib/auth";
export async function cancelMyBookingAction(formData: FormData) {

    const token = await getToken();

    if (!token) {

        redirect("/logg-inn?neste=/mine-bestillinger");

    }

    const bookingId = String(formData.get("bookingId") ?? "");

    const result = await cancelMyBooking(token, bookingId);

    redirect(result.ok

        ? "/mine-bestillinger?avbestilt=1"

        : `/mine-bestillinger?feil=${encodeURIComponent(result.message)}`);
}