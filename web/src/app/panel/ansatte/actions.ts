"use server";
import { redirect } from "next/navigation";
import { addEmployee, deleteEmployee, updateEmployee } from "@/lib/api";
import { getToken } from "@/lib/auth";

export async function addEmployeeAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await addEmployee(token, {
        name: String(formData.get("name") ?? ""),
        lastName: String(formData.get("lastName") ?? ""),
    });
    if (!result.ok) {
        redirect(`/panel/ansatte?feil=${encodeURIComponent(result.message)}`);
    }
    redirect("/panel/ansatte?lagt-til=1");
}

export async function updateEmployeeAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const employeeId = String(formData.get("employeeId") ?? "");
    const result = await updateEmployee(token, employeeId, {
        name: String(formData.get("name") ?? ""),
        lastName: String(formData.get("lastName") ?? ""),
    });
    if (!result.ok) {
        redirect(`/panel/ansatte?feil=${encodeURIComponent(result.message)}`);
    }
    redirect("/panel/ansatte?endret=1");
}

export async function deleteEmployeeAction(formData: FormData) {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const employeeId = String(formData.get("employeeId") ?? "");
    const result = await deleteEmployee(token, employeeId);
    if (!result.ok) {
        redirect(`/panel/ansatte?feil=${encodeURIComponent(result.message)}`);
    }
    redirect("/panel/ansatte?slettet=1");
}
