import { cookies } from "next/headers";

export const TOKEN_COOKIE = "besti2_token";
// The chosen booking time while a new customer confirms the e-mail (see registrer/actions.ts)
export const NEXT_COOKIE = "besti2_neste";
export async function getToken(): Promise<string | undefined> {
    const cookieStore = await cookies();
    return cookieStore.get(TOKEN_COOKIE)?.value;
}