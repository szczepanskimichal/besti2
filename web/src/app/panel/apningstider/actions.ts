"use server";

import { redirect } from "next/navigation";
import { updateOpeningHours } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { weekdays } from "@/lib/weekdays";

export async function updateOpeningHoursAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    // Only checked days are sent; the API treats missing days as closed
    const days = weekdays
        .filter((w) => formData.get(`open-${w.day}`) === "on")
        .map((w) => ({
            dayOfWeek: w.day,
            opensAt: String(formData.get(`opensAt-${w.day}`) ?? ""),
            closesAt: String(formData.get(`closesAt-${w.day}`) ?? ""),
        }));

    if (days.some((d) => d.opensAt === "" || d.closesAt === "")) {
        redirect(`/panel/apningstider?feil=${encodeURIComponent("Fyll ut åpnings- og stengetid for åpne dager.")}`);
    }

    const result = await updateOpeningHours(token, { days });
    if (!result.ok) {
        redirect(`/panel/apningstider?feil=${encodeURIComponent(result.message)}`);
    }

    redirect("/panel/apningstider?lagret=1");
}