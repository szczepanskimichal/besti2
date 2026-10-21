import type { MetadataRoute } from "next";
import { SITE_URL } from "@/lib/site";

// /robots.txt: search engines may read the public pages, not the private ones
export default function robots(): MetadataRoute.Robots {
    return {
        rules: {
            userAgent: "*",
            allow: "/",
            disallow: ["/panel", "/min-besti", "/logg-inn", "/registrer$", "/registrer?", "/glemt-passord", "/bekreft-epost", "/bedrift/*/bestill", ],
        },
        sitemap: `${SITE_URL}/sitemap.xml`,
    };
}