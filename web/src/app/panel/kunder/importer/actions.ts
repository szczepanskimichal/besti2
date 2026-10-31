"use server";

import { redirect } from "next/navigation";
import { importCustomers } from "@/lib/api";
import { getToken } from "@/lib/auth";
import type { CustomerImportResult } from "@/lib/types";
import type { MappedRow } from "./csv";

export type ImportState = { result?: CustomerImportResult; error?: string };

// save = false: the check before "Importer" (counts and problems, nothing is saved)
export async function importCustomersAction(rows: MappedRow[], save: boolean, confirmed: boolean):
    Promise<ImportState> {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await importCustomers(token, rows, save, confirmed);
    return result.ok ? { result: result.result } : { error: result.message };
}
