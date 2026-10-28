"use server";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { registerCustomer } from "@/lib/api";
import type { FormState } from "@/lib/formState";
import { NEXT_COOKIE } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";

export async function registerCustomerAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const fullName = String(formData.get("fullName") ?? "");
    const email = String(formData.get("email") ?? "");
    const result = await registerCustomer({
        fullName,
        email,
        password: String(formData.get("password") ?? ""),
    });
    if (!result.ok) {
        return { error: result.message, values: { fullName, email } };
    }
    // Signed up in the middle of a booking: remember the chosen time for 1 hour,
    // so "Logg inn" after confirming the e-mail goes straight back to it
    const next = safeRedirectPath(String(formData.get("neste") ?? ""));
    if (next !== "/") {
        const cookieStore = await cookies();
        cookieStore.set(NEXT_COOKIE, next, {
            httpOnly: true,
            secure: process.env.NODE_ENV === "production",
            sameSite: "lax",
            path: "/",
            maxAge: 60 * 60,
        });
    }
    redirect("/registrer?sendt=1");
}
