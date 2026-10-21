"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getMe, login } from "@/lib/api";
import { TOKEN_COOKIE } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";
import { FormState } from "@/lib/formState";

export async function loginAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    const result = await login(email, password);
    if (result === "tooMany") {
        return { error: "For mange forsøk. Prøv igjen om litt.", values: { email } };
    }
    if (!result) {
        return {
            error: "Feil e-post eller passord. Har du nettopp opprettet konto? Bekreft e-posten først.",
            values: { email },
        };
    }

    const cookieStore = await cookies();
    cookieStore.set(TOKEN_COOKIE, result.accessToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: result.expiresIn,
    });
    // Customers go back to where they were (e.g. the chosen time), businesses to their panel
    const me = await getMe(result.accessToken);
    const safeNext = safeRedirectPath(String(formData.get("neste") ?? ""));
    redirect(me?.isBusiness ? "/panel" : safeNext);
}

export async function logoutAction() {
    const cookieStore = await cookies();
    cookieStore.delete(TOKEN_COOKIE);
    redirect("/logg-inn");
}
