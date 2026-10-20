"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getMe, login } from "@/lib/api";
import { TOKEN_COOKIE } from "@/lib/auth";

export async function loginAction(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    const result = await login(email, password);

    if(result === "tooMany") {
        redirect("/logg-inn?feil=for-mange");
    }
    if (!result) {
        redirect("/logg-inn?feil=1");
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
    const next = String(formData.get("neste") ?? "");
    // "//evil.com" and "/\evil.com" would send the user to another site
    const safeNext = next.startsWith("/") && !next.startsWith("//") && !next.startsWith("/\\") ? next : "/";
    redirect(me?.isBusiness ? "/panel" : safeNext);
}

export async function logoutAction() {
    const cookieStore = await cookies();
    cookieStore.delete(TOKEN_COOKIE);
    redirect("/logg-inn");
}
