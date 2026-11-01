// Mirrors the BookingStatus enum in besti2.Domain; shared by the panel and Mine bestillinger
export const statusLabels: Record<string, string> = {
    Venter: "Venter på bekreftelse",
    Bekreftet: "Bekreftet",
    Avlyst: "Avlyst",
    VenterKunde: "Venter på kunden",
    MotteIkke: "Møtte ikke",
};

// The customer's name in the calendar; the people icon in front (PersonsIcon) shows one, two or a group
export function customerLabel(booking: { customerName: string }): string {
    return booking.customerName;
}
