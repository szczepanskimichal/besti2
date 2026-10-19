import { NextResponse, type NextRequest } from "next/server";
import { TOKEN_COOKIE } from "@/lib/auth";
// Runs before every /panel page: without the login cookie, go straight to the login page
export function proxy(request: NextRequest) {

    if (!request.cookies.has(TOKEN_COOKIE)) {

        return NextResponse.redirect(new URL("/logg-inn", request.url));

    }

    return NextResponse.next();
}
// Only /panel and everything under it
export const config = {

    matcher: ["/panel/:path*"],
};