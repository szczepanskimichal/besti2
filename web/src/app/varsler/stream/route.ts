import { type NextRequest } from "next/server";
import { openNotificationStream } from "@/lib/api";
import { getToken } from "@/lib/auth";

export const dynamic = "force-dynamic";

// Passes the API's live stream on to the bell in the browser (the token stays in the cookie on the server)
export async function GET(request: NextRequest) {
    const token = await getToken();
    if (!token) {
        return new Response(null, { status: 401 });
    }
    const upstream = await openNotificationStream(token, request.signal).catch(() => null);
    if (!upstream?.ok || !upstream.body) {
        return new Response(null, { status: upstream?.status ?? 503 });
    }
    return new Response(upstream.body, {
        headers: {
            "Content-Type": "text/event-stream",
            "Cache-Control": "no-cache, no-transform",
            Connection: "keep-alive",
        },
    });
}
