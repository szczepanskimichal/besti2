"use server";

import { redirect } from "next/navigation";
import { updateOpeningHours } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";
import { weekdays } from "@/lib/weekdays";

// Errors come back to the form with all 7 days as they were typed (no text in the URL)
export async function updateOpeningHoursAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const w of weekdays) {
        for (const field of [`open-${w.day}`, `opensAt-${w.day}`, `closesAt-${w.day}`]) {
            values[field] = String(formData.get(field) ?? "");
        }
    }

    // Only checked days are sent; the API treats missing days as closed
    const days = weekdays
        .filter((w) => values[`open-${w.day}`] === "on")
        .map((w) => ({
            dayOfWeek: w.day,
            opensAt: values[`opensAt-${w.day}`],
            closesAt: values[`closesAt-${w.day}`],
        }));

    if (days.some((d) => d.opensAt === "" || d.closesAt === "")) {
        return { error: "Fyll ut åpnings- og stengetid for åpne dager.", values };
    }

    const result = await updateOpeningHours(token, { days });
    if (!result.ok) {
        return { error: result.message, values };
    }

    redirect("/panel/apningstider?lagret=1");
}
