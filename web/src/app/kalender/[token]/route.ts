import { getCalendarFeed } from "@/lib/api";

export const dynamic = "force-dynamic";

// The salon's bookings for its calendar app (bestiapp.no/kalender/{secret}.ics). Google, Apple and Outlook
// fetch it by themselves without a login; the API checks the secret address
export async function GET(_request: Request, { params }: { params: Promise<{ token: string }> }) {
    const { token } = await params;
    const ics = await getCalendarFeed(token.replace(/\.ics$/, ""));
    if (!ics) {
        return new Response("Fant ikke kalenderen", { status: 404 });
    }
    return new Response(ics, {
        headers: {
            "Content-Type": "text/calendar; charset=utf-8",
            "Cache-Control": "no-store",
            "X-Robots-Tag": "noindex",
        },
    });
}
