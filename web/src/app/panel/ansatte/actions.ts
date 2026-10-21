"use server";
import { redirect } from "next/navigation";
import { addEmployee, deleteEmployee, updateEmployee } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { FormState } from "@/lib/formState";

// All three return errors to the form (no text in the URL); success reloads the page with a short code
export async function addEmployeeAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await addEmployee(token, employeeInput(formData));
    if (!result.ok) {
        return { error: result.message, values: employeeInput(formData) };
    }
    redirect("/panel/ansatte?lagt-til=1");
}

export async function updateEmployeeAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const employeeId = String(formData.get("employeeId") ?? "");
    const result = await updateEmployee(token, employeeId, employeeInput(formData));
    if (!result.ok) {
        return { error: result.message, values: employeeInput(formData) };
    }
    redirect("/panel/ansatte?endret=1");
}

export async function deleteEmployeeAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const employeeId = String(formData.get("employeeId") ?? "");
    const result = await deleteEmployee(token, employeeId);
    if (!result.ok) {
        return { error: result.message };
    }
    redirect("/panel/ansatte?slettet=1");
}

function employeeInput(formData: FormData) {
    return {
        name: String(formData.get("name") ?? ""),
        lastName: String(formData.get("lastName") ?? ""),
    };
}
