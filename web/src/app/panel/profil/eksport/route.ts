import { NextResponse } from "next/server";
import { exportMyData } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "Last ned mine data" for a business owner: the login and the business as a file to save (GDPR art. 15 and 20).
// The customers' data belongs to the business and is in Kunder (CSV)
export async function GET(request: Request) {
    const token = await getToken();
    const data = token ? await exportMyData(token) : null;
    if (!data) {
        return NextResponse.redirect(new URL("/logg-inn?neste=/panel/profil", request.url));
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
