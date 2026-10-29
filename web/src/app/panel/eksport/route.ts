import { exportBookingsCsv } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { isIsoDate } from "@/lib/format";

// "Last ned bestillinger" in Statistikk: the CSV file from the API, downloaded with the login cookie
export async function GET(request: Request) {
    const token = await getToken();
    if (!token) {
        return Response.redirect(new URL("/logg-inn", request.url));
    }
    const params = new URL(request.url).searchParams;
    const from = params.get("fra") ?? undefined;
    const to = params.get("til") ?? undefined;
    if (!isIsoDate(from) || !isIsoDate(to)) {
        return new Response("Velg fra- og til-dato.", { status: 400 });
    }

    const file = await exportBookingsCsv(token, from, to);
    if (!file) {
        return new Response("Velg en periode på maks to år, der fra-datoen er før til-datoen.", { status: 400 });
    }
    return new Response(await file.arrayBuffer(), {
        headers: {
            "Content-Type": "text/csv; charset=utf-8",
            "Content-Disposition": `attachment; filename="bestillinger-${from}-${to}.csv"`,
            "Cache-Control": "no-store",
        },
    });
}
