import type { MetadataRoute } from "next";
import { ALLOW_INDEXING, SITE_URL } from "@/lib/site";

// /robots.txt: search engines may read the public pages, not the private ones
export default function robots(): MetadataRoute.Robots {
    // Before launch: no search engine may read anything
    if (!ALLOW_INDEXING) {
        return { rules: { userAgent: "*", disallow: "/" } };
    }
    return {
        rules: {
            userAgent: "*",
            allow: "/",
            // Login, sign-up and password pages are not blocked: Google must be able to read their "noindex",
            // or a blocked page can still show up in Google as a bare address
            disallow: ["/panel", "/min-besti", "/ansatt", "/admin", "/varsler", "/*/bestill"],
        },
        sitemap: `${SITE_URL}/sitemap.xml`,
    };
}