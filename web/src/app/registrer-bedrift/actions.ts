"use server";

import { redirect } from "next/navigation";
import { registerBusiness } from "@/lib/api";

const FIELDS = ["businessName", "orgNumber", "category", "email", "phone", "address", "postalCode", "city", "password"];

export async function registerAction(formData: FormData) {
    const input: Record<string, string> = {};
    for (const field of FIELDS) {
        input[field] = String(formData.get(field) ?? "");
    }

    const result = await registerBusiness(input);

    if (!result.ok) {
        redirect(`/registrer-bedrift?feil=${encodeURIComponent(result.message)}`);
    }

    redirect("/logg-inn?registrert=1");
}