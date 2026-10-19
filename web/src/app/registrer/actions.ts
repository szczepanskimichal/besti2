"use server";
import { redirect } from "next/navigation";
import { registerCustomer } from "@/lib/api";

export async function registerCustomerAction(formData: FormData) {
    const result = await registerCustomer({
        fullName: String(formData.get("fullName") ?? ""),
        email: String(formData.get("email") ?? ""),
        password: String(formData.get("password") ?? ""),
    });
    if (!result.ok) {
        redirect(`/registrer?feil=${encodeURIComponent(result.message)}`);
    }
    redirect("/registrer?sendt=1");
}
