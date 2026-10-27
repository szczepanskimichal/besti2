namespace besti2.Domain.Enums;

public enum BookingStatus
{
    Venter,
    Bekreftet,
    Avlyst,

    // The business moved the visit and waits for the customer to accept the new time
    VenterKunde,

    // The customer did not come; the business marks it after the start time
    MotteIkke,
}