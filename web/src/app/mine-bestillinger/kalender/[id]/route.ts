import { getMyBookings } from "@/lib/api";
import { getToken } from "@/lib/auth";

// "2026-10-24T08:00:00Z" -> "20261024T080000Z" (iCalendar UTC format)
function toIcsDate(utc: string): string {
    return new Date(utc).toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
}
// Commas, semicolons, backslashes and new lines must be escaped in iCalendar text
function escapeIcs(text: string): string {
    return text.replace(/\\/g, "\\\\").replace(/([,;])/g, "\\$1").replace(/\n/g, "\\n");
}
// Downloads one of the customer's own bookings as an .ics file for the phone's calendar
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
    const { id } = await params;
    const token = await getToken();
    const bookings = token ? await getMyBookings(token) : null;
    const booking = bookings?.find((b) => b.id === id);
    if (!booking) {
        return new Response("Fant ikke bestillingen", { status: 404 });
    }
    const ics = [
        "BEGIN:VCALENDAR",
        "VERSION:2.0",
        "PRODID:-//BESTI//NO",
        "BEGIN:VEVENT",
        `UID:${booking.id}@bestiapp.no`,
        `DTSTAMP:${toIcsDate(new Date().toISOString())}`,
        `DTSTART:${toIcsDate(booking.startUtc)}`,
        `DTEND:${toIcsDate(booking.endUtc)}`,
        `SUMMARY:${escapeIcs(`${booking.serviceName} – ${booking.businessName}`)}`,
        `LOCATION:${escapeIcs(booking.businessAddress)}`,
        `DESCRIPTION:${escapeIcs(`Hos ${booking.employeeName}. Bestilt via BESTI.`)}`,
        "END:VEVENT",
        "END:VCALENDAR",
    ].join("\r\n");
    return new Response(ics, {
        headers: {
            "Content-Type": "text/calendar; charset=utf-8",
            "Content-Disposition": `attachment; filename="besti-${id.slice(0, 8)}.ics"`,
        },
    });
}