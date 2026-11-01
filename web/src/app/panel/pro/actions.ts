"use server";

import { redirect } from "next/navigation";
import { sendProInterest } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "Jeg vil være med" at the bottom of BESTI Pro: the same Pro interest as under Statistikk and Del
export async function proPageInterestAction() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await sendProInterest(token);
    redirect(result.ok ? "/panel/pro?pro=1#med" : "/panel/pro?feil=1#med");
}
