import { NextResponse, type NextRequest } from "next/server";
import { markNotificationRead } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { safeRedirectPath } from "@/lib/safeRedirect";

// A relative Location: the browser stays on the address it used (phone IP, bestiapp.no)
function goTo(path: string) {
    return new NextResponse(null, { status: 307, headers: { Location: path } });
}
// A click on one notification: only that one is marked as read, then the visit / page it is about opens
export async function GET(request: NextRequest, { params }: { params: Promise<{ id: string }> }) {
    const token = await getToken();
    if (!token) {
        return goTo("/logg-inn?neste=/varsler");
    }
    const { id } = await params;
    const link = await markNotificationRead(token, id);
    return goTo(safeRedirectPath(link ?? "/varsler"));
}