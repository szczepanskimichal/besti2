import type { CustomerBooking } from "@/lib/types";

// Same choices as the API (Business.FreeCancelOptions); 0 = any time before the visit
export const FREE_CANCEL_OPTIONS = [0, 2, 12, 24, 48];

export function freeCancelText(hours: number): string {
    return hours === 0
        ? "Gratis avbestilling når som helst før timen"
        : `Gratis avbestilling inntil ${hours} timer før timen`;
}

// Same rule as the API: a confirmed visit only until the deadline, a visit still waiting for the business always
export function canCancelOnline(booking: CustomerBooking): boolean {
    if (booking.status !== "Bekreftet") {
        return true;
    }
    const deadline = new Date(booking.startUtc).getTime() - booking.freeCancelHours * 60 * 60 * 1000;
    return Date.now() < deadline;
}
