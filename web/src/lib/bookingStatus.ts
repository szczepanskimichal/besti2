// Mirrors the BookingStatus enum in besti2.Domain; shared by the panel and Mine bestillinger
export const statusLabels: Record<string, string> = {
    Venter: "Venter på bekreftelse",
    Bekreftet: "Bekreftet",
    Avlyst: "Avlyst",
    VenterKunde: "Venter på kunden",
    MotteIkke: "Møtte ikke",
};

// BESTI sammen: the two visits of one invitation are marked with 👯 in the calendar
export function customerLabel(booking: { customerName: string; inviteId: string | null }): string {
    return booking.inviteId ? `👯 ${booking.customerName}` : booking.customerName;
}
