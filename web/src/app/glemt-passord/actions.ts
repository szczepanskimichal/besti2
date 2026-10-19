"use server";

import { redirect } from "next/navigation";
import { forgotPassword, resetPassword } from "@/lib/api";

export async function forgotPasswordAction(formData: FormData) {
    const email = String(formData.get("email") ?? "").trim();
    if (email) {
        await forgotPassword(email);
    }
    redirect("/glemt-passord?sendt=1");
}
export async function resetPasswordAction(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const code = String(formData.get("code") ?? "");
    const password = String(formData.get("password") ?? "");
    const ok = await resetPassword(email, code, password);
    if (!ok) {
        const back = `/glemt-passord/ny?epost=${encodeURIComponent(email)}&kode=${encodeURIComponent(code)}&feil=1`;
        redirect(back);
    }
    redirect("/logg-inn?nyttPassord=1");
}
