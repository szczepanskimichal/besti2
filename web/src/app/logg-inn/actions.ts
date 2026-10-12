"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { login } from "@/lib/api";
import { TOKEN_COOKIE } from "@/lib/auth";

export async function loginAction(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    const result = await login(email, password);

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

    redirect("/panel");
}

export async function logoutAction() {
    const cookieStore = await cookies();
    cookieStore.delete(TOKEN_COOKIE);
    redirect("/logg-inn");
}
