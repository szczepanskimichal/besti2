import { createHash, randomBytes } from "node:crypto";
import { cookies } from "next/headers";
import { safeRedirectPath } from "@/lib/safeRedirect";
import { GOOGLE_CLIENT_ID, GOOGLE_LOGIN_ENABLED, SITE_URL } from "@/lib/site";
import { GOOGLE_COOKIE, GOOGLE_REDIRECT_URI } from "./googleLogin";

// Step 1: send the visitor to Google. state stops someone else's login from being slipped in,
// the PKCE verifier makes a stolen code useless; both wait in a short cookie until Google sends the visitor back
export async function GET(request: Request) {
    if (!GOOGLE_LOGIN_ENABLED) {
        return Response.redirect(new URL("/logg-inn", SITE_URL), 303);
    }

    const next = safeRedirectPath(new URL(request.url).searchParams.get("neste") ?? "/");
    const state = randomBytes(32).toString("base64url");
    const verifier = randomBytes(32).toString("base64url");
    (await cookies()).set(GOOGLE_COOKIE, JSON.stringify({ state, verifier, next }), {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/logg-inn/google",
        maxAge: 10 * 60,
    });

    const google = new URL("https://accounts.google.com/o/oauth2/v2/auth");
    google.search = new URLSearchParams({
        client_id: GOOGLE_CLIENT_ID,
        redirect_uri: GOOGLE_REDIRECT_URI,
        response_type: "code",
        scope: "openid email profile",
        state,
        code_challenge: createHash("sha256").update(verifier).digest("base64url"),
        code_challenge_method: "S256",
        prompt: "select_account",
    }).toString();
    return Response.redirect(google, 303);
}
