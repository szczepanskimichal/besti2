import { NextResponse } from "next/server";
import { exportMyData } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "Last ned mine data": the customer's data as a file to save (GDPR art. 15 and 20)
export async function GET(request: Request) {
    const token = await getToken();
    const data = token ? await exportMyData(token) : null;
    if (!data) {
        return NextResponse.redirect(new URL("/logg-inn?neste=/min-besti/profil", request.url));
    }
    const day = new Date().toISOString().slice(0, 10);
    return new NextResponse(JSON.stringify(data, null, 2), {
        headers: {
            "Content-Type": "application/json; charset=utf-8",
            "Content-Disposition": `attachment; filename="besti-mine-data-${day}.json"`,
            "Cache-Control": "no-store",
        },
    });
}
