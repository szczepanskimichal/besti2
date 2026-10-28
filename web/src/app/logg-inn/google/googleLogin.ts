import { SITE_URL } from "@/lib/site";

// "Fortsett med Google" (OAuth 2.0 code flow with PKCE), shared by the two routes in this folder
export const GOOGLE_COOKIE = "besti2_google";
// Google's message for the login page when something went wrong (shown once, see logg-inn/page.tsx)
export const GOOGLE_ERROR_COOKIE = "besti2_google_feil";
// Must be exactly the same as in Google Cloud → Credentials → Authorized redirect URIs
export const GOOGLE_REDIRECT_URI = `${SITE_URL}/logg-inn/google/callback`;
