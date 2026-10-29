"use server";

import { redirect } from "next/navigation";
import { setCustomerNote } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// Empty text removes the note
export async function customerNoteAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const key = String(formData.get("key") ?? "");
    const note = String(formData.get("note") ?? "");
    const result = await setCustomerNote(token, key, note);
    if (!result.ok) {
        return { error: result.message, values: { note } };
    }
    redirect(`/panel/kunder/${encodeURIComponent(key)}?lagret=1`);
}
