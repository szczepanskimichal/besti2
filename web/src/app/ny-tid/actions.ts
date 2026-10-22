"use server";
import { redirect } from "next/navigation";
import { answerNewTime } from "@/lib/api";

// The answer from the e-mail link; the token is used once, so the result page does not need it any more
export async function answerNewTimeAction(formData: FormData) {
    const accept = formData.get("svar") === "ja";
    const result = await answerNewTime(String(formData.get("token") ?? ""), accept);
    redirect(result.ok ? `/ny-tid?svar=${accept ? "godtatt" : "avslatt"}` : "/ny-tid?svar=feil");
}
