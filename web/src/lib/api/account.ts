import {cache} from "react";
import {apiFetch, toResult, type ApiResult} from "./client";

// Login, customer account, password and Min profil

// null = wrong e-mail or password (or the e-mail is not confirmed yet)
export type LoginProblem = "tooMany" | "lockedOut" | "noContact";

export async function login(email: string, password: string): Promise<{ accessToken: string; expiresIn: number } | null | LoginProblem> {
    const res = await apiFetch("/api/auth/login", { method: "POST", body: { email, password }, forwardIp: true });
    // Too many attempts: the API's rate limiter answered 429
    if (res.status === 429) {
        return "tooMany";
    }
    // API down or too slow (safeFetch answers 503)
    if (res.status >= 500) {
        return "noContact";
    }
    if (!res.ok) {
        // Identity says "LockedOut" after 5 wrong passwords in a row (the account waits 5 minutes)
        const body = await res.json().catch(() => null);
        return String(body?.detail ?? "").toLowerCase() === "lockedout" ? "lockedOut" : null;
    }
    return res.json();
}

// "Fortsett med Google": the ID token from Google in, BESTI's login token out (or the API's message)
export async function loginWithGoogle(idToken: string): Promise<{ ok: true; accessToken: string; expiresIn: number } | { ok: false; message: string }> {
    const res = await apiFetch("/api/auth/google", { method: "POST", body: { idToken }, forwardIp: true });
    const body = await res.json().catch(() => null);
    if (res.ok && body?.accessToken) {
        return { ok: true, accessToken: body.accessToken, expiresIn: body.expiresIn };
    }
    return {
        ok: false,
        message: res.status === 429 ? "For mange forsøk. Prøv igjen om litt." : body?.message ?? "Kunne ikke logge inn med Google. Prøv igjen.",
    };
}

// Always "ok": the API never says whether the e-mail exists (so nobody can check who has an account)
export async function forgotPassword(email: string): Promise<void> {
    await apiFetch("/api/auth/forgotPassword", { method: "POST", body: { email }, forwardIp: true });
}

export async function resetPassword(email: string, resetCode: string, newPassword: string): Promise<boolean> {
    const res = await apiFetch("/api/auth/resetPassword", { method: "POST", body: { email, resetCode, newPassword }, forwardIp: true });
    return res.ok;
}

export async function registerCustomer(input: { fullName: string; email: string; password: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/account/register", { method: "POST", body: input, forwardIp: true }));
}

export async function confirmEmail(userId: string, code: string): Promise<boolean> {
    const res = await apiFetch("/api/account/confirm-email", { method: "POST", body: { userId, code }, forwardIp: true });
    return res.ok;
}

export type Me = {
    email: string;
    fullName: string | null;
    phone: string | null; // from "Min profil"; fills in the booking form
    isBusiness: boolean;
    isAdmin: boolean; // the BESTI team: the header shows a link to /admin
};

// null = not logged in (401/403). Other errors (API down) are thrown, so a logged-in business
// sees the error page instead of being sent to the login page again and again
export const getMe = cache(async (token: string): Promise<Me | null> => {
    const res = await apiFetch("/api/me", { token });
    if (res.ok) return res.json();
    if (res.status === 401 || res.status === 403) return null;
    throw new Error(`Kunne ikke hente brukeren (${res.status})`);
});

// "Min profil" (customer)
// hasPassword = false for an account made with "Fortsett med Google"
export type MyProfile = { fullName: string; email: string; phone: string | null; hasPassword: boolean };

export async function getMyProfile(token: string): Promise<MyProfile | null> {
    const res = await apiFetch("/api/me/profile", { token });
    return res.ok ? res.json() : null;
}

export async function updateMyProfile(token: string, input: { fullName: string; phone: string | null }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/profile", { method: "PUT", token, body: input }));
}

export async function changeMyPassword(token: string, input: { currentPassword: string; newPassword: string }): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/password", { method: "POST", token, body: input, forwardIp: true }));
}

// "Slett konto"; confirmed with the password
export async function deleteMyAccount(token: string, password: string): Promise<ApiResult> {
    return toResult(await apiFetch("/api/me/delete", { method: "POST", token, body: { password }, forwardIp: true }));
}

// "Last ned mine data": everything BESTI keeps about the logged-in customer (null = not a customer)
export async function exportMyData(token: string): Promise<unknown | null> {
    const res = await apiFetch("/api/me/export", { token });
    return res.ok ? res.json() : null;
}
