import { cookies } from "next/headers";
import { getMe, loginWithGoogle } from "@/lib/api";
import { setToken } from "@/lib/auth";
import { GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, GOOGLE_LOGIN_ENABLED, SITE_URL } from "@/lib/site";
import { GOOGLE_COOKIE, GOOGLE_ERROR_COOKIE, GOOGLE_REDIRECT_URI } from "../googleLogin";

type Started = { state: string; verifier: string; next: string };

// Step 2: Google sends the visitor back with a code. We swap it for an ID token (server to server, with the
// secret), the API checks the token and gives BESTI's own login, and the visitor goes on like after a normal login
export async function GET(request: Request) {
    const params = new URL(request.url).searchParams;
    const cookieStore = await cookies();
    const started = readStarted(cookieStore.get(GOOGLE_COOKIE)?.value);
    cookieStore.delete({ name: GOOGLE_COOKIE, path: "/logg-inn/google" });

    // Pressed "Avbryt" at Google: back to the login page without an error
    if (params.get("error") === "access_denied") {
        return goTo("/logg-inn");
    }

    const code = params.get("code");
    if (!GOOGLE_LOGIN_ENABLED || !started || !code || params.get("state") !== started.state) {
        return fail("Innloggingen med Google ble avbrutt. Prøv igjen.");
    }

    const idToken = await exchangeCode(code, started.verifier);
    if (!idToken) {
        return fail("Vi fikk ikke kontakt med Google. Prøv igjen, eller logg inn med e-post og passord.");
    }

    const result = await loginWithGoogle(idToken);
    if (!result.ok) {
        return fail(result.message);
    }

    await setToken(result.accessToken, result.expiresIn);
    // Customers go back to where they were (e.g. the chosen time), businesses to their panel
    const me = await getMe(result.accessToken);
    return goTo(me?.isBusiness ? "/panel" : started.next);
}

function readStarted(value: string | undefined): Started | null {
    try {
        const parsed = value ? JSON.parse(value) : null;
        return typeof parsed?.state === "string" && typeof parsed?.verifier === "string" && typeof parsed?.next === "string"
            ? parsed
            : null;
    } catch {
        return null;
    }
}

async function exchangeCode(code: string, verifier: string): Promise<string | null> {
    try {
        const res = await fetch("https://oauth2.googleapis.com/token", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                code,
                client_id: GOOGLE_CLIENT_ID,
                client_secret: GOOGLE_CLIENT_SECRET,
                redirect_uri: GOOGLE_REDIRECT_URI,
                grant_type: "authorization_code",
                code_verifier: verifier,
            }),
            cache: "no-store",
            signal: AbortSignal.timeout(8000),
        });
        const body = await res.json().catch(() => null);
        return res.ok && typeof body?.id_token === "string" ? body.id_token : null;
    } catch {
        return null;
    }
}

// The public address, not request.url (behind the web server that is the inside address)
function goTo(path: string) {
    return Response.redirect(new URL(path, SITE_URL), 303);
}

// The message waits in a cookie for one minute; a message in the address could be faked by anyone
async function fail(message: string) {
    (await cookies()).set(GOOGLE_ERROR_COOKIE, message, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/logg-inn",
        maxAge: 60,
    });
    return goTo("/logg-inn");
}
