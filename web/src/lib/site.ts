import type { Metadata } from "next";

// The public address of the site (https://bestiapp.no in production; set SITE_URL when deploying)
export const SITE_URL = process.env.SITE_URL ?? "http://localhost:3000";

// Google may index the site only when ALLOW_INDEXING=true (GitHub → Settings → Variables; off until real businesses are in)
export const ALLOW_INDEXING = process.env.ALLOW_INDEXING === "true";

// Pages that belong to one person (login, panel, Min Besti, booking): never in Google
export const privatePage: Metadata["robots"] = { index: false, follow: false };
