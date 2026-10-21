import type { Metadata } from "next";

// The public address of the site (https://bestiapp.no in production; set SITE_URL when deploying)
export const SITE_URL = process.env.SITE_URL ?? "http://localhost:3000";

// Pages that belong to one person (login, panel, Min Besti, booking): never in Google
export const privatePage: Metadata["robots"] = { index: false, follow: false };