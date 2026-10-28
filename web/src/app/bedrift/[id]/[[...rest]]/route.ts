import { getBusiness } from "@/lib/api";

// Old links from before the short addresses (/bedrift/<id>, /bedrift/<id>/bestill?...) in e-mails, the bell
// and on Google: moved for good (308) to /<slug>, so they keep working
export async function GET(request: Request, { params }: { params: Promise<{ id: string; rest?: string[] }> }) {
    const { id, rest } = await params;
    const business = /^[0-9a-f-]{36}$/i.test(id) ? await getBusiness(id) : null;
    // Deleted or unknown business: the front page instead of an error
    if (!business) {
        return new Response(null, { status: 307, headers: { Location: "/" } });
    }

    // Only the known sub pages (bestill, bestill/bekreftet) and the same ?tjeneste=... as before
    const path = [business.slug, ...(rest ?? []).filter((part) => /^[a-z]+$/.test(part))].join("/");
    const { search } = new URL(request.url);
    // A relative address: behind the web server request.url is the inside address, not bestiapp.no
    return new Response(null, { status: 308, headers: { Location: `/${path}${search}` } });
}
