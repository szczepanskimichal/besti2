import type { Metadata } from "next";

// The public address of the site (https://bestiapp.no in production; set SITE_URL when deploying)
export const SITE_URL = process.env.SITE_URL ?? "http://localhost:3000";

// Google may index the site only when ALLOW_INDEXING=true (GitHub → Settings → Variables; off until real businesses are in)
export const ALLOW_INDEXING = process.env.ALLOW_INDEXING === "true";

// Visit statistics (Umami, without cookies): the site's id from Umami; empty = no statistics (local, tests)
export const UMAMI_WEBSITE_ID = process.env.UMAMI_WEBSITE_ID ?? "";
// Umami Cloud (EU) by default; a self-hosted Umami has its own address
export const UMAMI_SCRIPT_URL = process.env.UMAMI_SCRIPT_URL ?? "https://cloud.umami.is/script.js";

// Pages that belong to one person (login, panel, Min Besti, booking): never in Google
export const privatePage: Metadata["robots"] = { index: false, follow: false };
