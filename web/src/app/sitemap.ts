import type { MetadataRoute } from "next";
import { getBusinesses } from "@/lib/api";
import { SITE_URL } from "@/lib/site";

// /sitemap.xml: the start page, the info pages and every business page, so Google finds them
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
    const pages: MetadataRoute.Sitemap = [
        { url: SITE_URL, changeFrequency: "daily", priority: 1 },
        { url: `${SITE_URL}/registrer-bedrift`, changeFrequency: "monthly" },
        { url: `${SITE_URL}/vilkar`, changeFrequency: "yearly" },
        { url: `${SITE_URL}/personvern`, changeFrequency: "yearly" },
    ];

    // The list comes 24 at a time, so read every page
    let page = 1;
    let pageCount = 1;
    do {
        const result = await getBusinesses(undefined, undefined, undefined, undefined, undefined, undefined,
            undefined, undefined, undefined, String(page));
        for (const business of result.items) {
            pages.push({ url: `${SITE_URL}/bedrift/${business.id}`, changeFrequency: "weekly" });
        }
        pageCount = result.pageCount;
        page++;
    } while (page <= pageCount);

    return pages;
}
