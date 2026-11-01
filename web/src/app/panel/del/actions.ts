"use server";

import { redirect } from "next/navigation";
import { sendProInterest } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "Jeg vil være med" under the SMS note: the same Pro interest as under Statistikk, back to the message card
export async function smsProInterestAction() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn");
    }
    const result = await sendProInterest(token);
    redirect(result.ok ? "/panel/del?pro=1#melding" : "/panel/del?feil=1#melding");
}
