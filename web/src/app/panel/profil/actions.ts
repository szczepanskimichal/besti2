"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { changeMyPassword, deleteMyBusiness, getMe, login, updateProfile, uploadLogo } from "@/lib/api";
import { getToken, setToken, TOKEN_COOKIE } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// saved = show "Passordet er endret" under the form
export type AccountFormState = FormState & { saved?: boolean };

// Errors come back to the form with the typed values (no text in the URL)
export async function updateProfileAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }

    const values: Record<string, string> = {};
    for (const field of ["description", "phone", "contactEmail", "address", "postalCode", "city", "freeCancelHours", "website", "autoConfirm"]) {
        values[field] = String(formData.get(field) ?? "");
    }

    const description = values.description.trim();
    const result = await updateProfile(token, {
        description: description === "" ? null : description,
        phone: values.phone,
        contactEmail: values.contactEmail.trim() === "" ? null : values.contactEmail,
        address: values.address,
        postalCode: values.postalCode,
        city: values.city,
        freeCancelHours: Number(values.freeCancelHours || 24),
        website: values.website.trim() === "" ? null : values.website,
        autoConfirm: values.autoConfirm === "true",
    });

    if (!result.ok) {
        return { error: result.message, values };
    }

    redirect("/panel/profil?lagret=1");
}

export async function uploadLogoAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const file = formData.get("logo");
    if (!(file instanceof File) || file.size === 0) {
        return { error: "Velg et bilde." };
    }
    const result = await uploadLogo(token, file);
    if (!result.ok) {
        return { error: result.message };
    }
    redirect("/panel/profil?logo=1");
}

// Same as in Min Besti: a new password logs out every device, so the owner is logged in again right away
export async function changePasswordAction(_previous: AccountFormState, formData: FormData): Promise<AccountFormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const newPassword = String(formData.get("newPassword") ?? "");
    if (newPassword !== String(formData.get("repeatPassword") ?? "")) {
        return { error: "De nye passordene er ikke like." };
    }
    const me = await getMe(token);
    const result = await changeMyPassword(token, {
        currentPassword: String(formData.get("currentPassword") ?? ""),
        newPassword,
    });
    if (!result.ok) {
        return { error: result.message };
    }
    const fresh = me ? await login(me.email, newPassword) : null;
    if (!fresh || fresh === "tooMany") {
        redirect("/logg-inn?neste=/panel/profil");
    }
    await setToken(fresh.accessToken, fresh.expiresIn);
    return { saved: true };
}

// Deleted: log out and show the goodbye page
export async function deleteBusinessAction(_previous: AccountFormState, formData: FormData): Promise<AccountFormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await deleteMyBusiness(token, String(formData.get("password") ?? ""));
    if (!result.ok) {
        return { error: result.message };
    }
    const cookieStore = await cookies();
    cookieStore.delete(TOKEN_COOKIE);
    redirect("/konto-slettet?bedrift=1");
}
