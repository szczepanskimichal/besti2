"use server";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { changeMyPassword, deleteMyAccount, updateMyProfile } from "@/lib/api";
import { getToken, TOKEN_COOKIE } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// saved = show "Lagret" under the form
export type ProfileFormState = FormState & { saved?: boolean };

async function tokenOrLogin(): Promise<string> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/profil");
    }
    return token;
}

export async function updateProfileAction(_previous: ProfileFormState, formData: FormData): Promise<ProfileFormState> {
    const token = await tokenOrLogin();
    const values = {
        fullName: String(formData.get("fullName") ?? ""),
        phone: String(formData.get("phone") ?? ""),
    };
    const result = await updateMyProfile(token, { fullName: values.fullName, phone: values.phone.trim() || null });
    return result.ok ? { saved: true, values } : { error: result.message, values };
}

// The passwords are never sent back to the form
export async function changePasswordAction(_previous: ProfileFormState, formData: FormData): Promise<ProfileFormState> {
    const token = await tokenOrLogin();
    const newPassword = String(formData.get("newPassword") ?? "");
    if (newPassword !== String(formData.get("repeatPassword") ?? "")) {
        return { error: "De nye passordene er ikke like." };
    }
    const result = await changeMyPassword(token, {
        currentPassword: String(formData.get("currentPassword") ?? ""),
        newPassword,
    });
    return result.ok ? { saved: true } : { error: result.message };
}

// Deleted: log out and show the goodbye page
export async function deleteAccountAction(_previous: ProfileFormState, formData: FormData): Promise<ProfileFormState> {
    const token = await tokenOrLogin();
    const result = await deleteMyAccount(token, String(formData.get("password") ?? ""));
    if (!result.ok) {
        return { error: result.message };
    }
    const cookieStore = await cookies();
    cookieStore.delete(TOKEN_COOKIE);
    redirect("/konto-slettet");
}
