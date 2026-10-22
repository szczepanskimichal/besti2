import { NextResponse, type NextRequest } from "next/server";
import { lookupBusinessRegistry } from "@/lib/api";

// For the "Hent fra Brønnøysund" button: asks our API, which asks Enhetsregisteret
export async function GET(request: NextRequest) {
    const orgNumber = request.nextUrl.searchParams.get("orgnr") ?? "";
    const result = await lookupBusinessRegistry(orgNumber);
    return result.ok
        ? NextResponse.json(result.business)
        : NextResponse.json({ message: result.message }, { status: 400 });
}