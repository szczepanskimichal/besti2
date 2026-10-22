import { NextResponse, type NextRequest } from "next/server";
import { markNotificationRead } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";

// A click on one notification: only that one is marked as read, then the visit / page it is about opens
export async function GET(request: NextRequest, { params }: { params: Promise<{ id: string }> }) {
    const token = await getToken();
    if (!token) {
        return NextResponse.redirect(new URL("/logg-inn?neste=/varsler", request.url));
    }
    const { id } = await params;
    const link = await markNotificationRead(token, id);
    return NextResponse.redirect(new URL(safeRedirectPath(link ?? "/varsler"), request.url));
}