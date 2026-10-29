"use server";
import { redirect } from "next/navigation";
import { addEmployee, deleteEmployee, inviteStaff, removeStaff, updateEmployee } from "@/lib/api";
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

// "Egen innlogging": the employee gets an e-mail with a link to connect their own BESTI account
export async function inviteStaffAction(_previous: FormState, formData: FormData): Promise<FormState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const email = String(formData.get("email") ?? "");
    const result = await inviteStaff(token, String(formData.get("employeeId") ?? ""), email);
    if (!result.ok) {
        return { error: result.message, values: { email } };
    }
    redirect("/panel/ansatte?invitert=1");
}

// Withdraw the invitation, or take the access away
export async function removeStaffAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    await removeStaff(token, String(formData.get("employeeId") ?? ""));
    redirect("/panel/ansatte?tilgang-fjernet=1");
}
