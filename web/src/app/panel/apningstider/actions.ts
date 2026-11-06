"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { updateOpeningHours } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";
import { weekdays } from "@/lib/weekdays";
import { HOURS_CHECKED_COOKIE } from "../firstStepsCookie";

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
        // The break row of the day (breakStart-1-0 ...); the -0 leaves room for more breaks later
        for (let i = 0; i < 1; i++) {
            for (const field of [`breakStart-${w.day}-${i}`, `breakEnd-${w.day}-${i}`]) {
                const value = formData.get(field);
                if (value !== null) {
                    values[field] = String(value);
                }
            }
        }
    }

    // Only checked days are sent; the API treats missing days as closed
    const days = weekdays
        .filter((w) => values[`open-${w.day}`] === "on")
        .map((w) => ({
            dayOfWeek: w.day,
            opensAt: values[`opensAt-${w.day}`],
            closesAt: values[`closesAt-${w.day}`],
            // An empty break = no break that day
            breakStart: values[`breakStart-${w.day}-0`] || null,
            breakEnd: values[`breakEnd-${w.day}-0`] || null,
        }));

    if (days.some((d) => d.opensAt === "" || d.closesAt === "")) {
        return { error: "Fyll ut åpnings- og stengetid for åpne dager.", values };
    }

    const result = await updateOpeningHours(token, { days });
    if (!result.ok) {
        return { error: result.message, values };
    }

    // Saved = checked, so "Sjekk åpningstidene" in Kom i gang is ticked even when the times stayed 09–17
    (await cookies()).set(HOURS_CHECKED_COOKIE, "1", {
        httpOnly: true,
        sameSite: "lax",
        secure: process.env.NODE_ENV === "production",
        path: "/panel",
        maxAge: 60 * 60 * 24 * 365,
    });
    redirect("/panel/apningstider?lagret=1");
}
