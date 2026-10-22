namespace besti2.Domain.Enums;

public enum BookingStatus
{
    Venter,
    Bekreftet,
    Avlyst,
    // The business moved the visit and waits for the customer to accept the new time
    VenterKunde,
}