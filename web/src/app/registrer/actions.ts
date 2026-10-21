"use server";
import { redirect } from "next/navigation";
import { registerCustomer } from "@/lib/api";
import type { FormState } from "@/lib/formState";

export async function registerCustomerAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const fullName = String(formData.get("fullName") ?? "");
    const email = String(formData.get("email") ?? "");
    const result = await registerCustomer({
        fullName,
        email,
        password: String(formData.get("password") ?? ""),
    });
    if (!result.ok) {
        return { error: result.message, values: { fullName, email } };
    }
    redirect("/registrer?sendt=1");
}
