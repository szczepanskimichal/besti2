"use server";

import { redirect } from "next/navigation";
import { removeImportedCustomer, setCustomerNote } from "@/lib/api";
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
    // From the notepad on Kunder: back to the list, the saved note stays open
    if (formData.get("tilbake") === "kunder") {
        redirect(`/panel/kunder?lagret=${encodeURIComponent(key)}`);
    }
    redirect(`/panel/kunder/${encodeURIComponent(key)}?lagret=1`);
}

// "Fjern fra Kunder": an imported customer who has not booked here, with the note
export async function removeCustomerAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const key = String(formData.get("key") ?? "");
    const removed = await removeImportedCustomer(token, key);
    redirect(removed ? "/panel/kunder" : `/panel/kunder/${encodeURIComponent(key)}`);
}
