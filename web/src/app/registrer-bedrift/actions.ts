"use server";

import { redirect } from "next/navigation";
import { registerBusiness } from "@/lib/api";
import type { FormState } from "@/lib/formState";

const FIELDS = ["businessName", "orgNumber", "category", "email", "phone", "address", "postalCode", "city", "password"];

export async function registerAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const input: Record<string, string> = {};
    for (const field of FIELDS) {
        input[field] = String(formData.get(field) ?? "");
    }

    const acceptDataProcessing = formData.get("acceptDataProcessing") === "on";
    const result = await registerBusiness({ ...input, acceptDataProcessing });

    if (!result.ok) {
        delete input.password; // never sent back to the browser
        return { error: result.message, values: { ...input, acceptDataProcessing: acceptDataProcessing ? "on" : "" } };
    }

    redirect("/logg-inn?registrert=1");
}