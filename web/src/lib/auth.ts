import { cookies } from "next/headers";

export const TOKEN_COOKIE = "besti2_token";

export async function getToken(): Promise<string | undefined> {
    const cookieStore = await cookies();
    return cookieStore.get(TOKEN_COOKIE)?.value;
}