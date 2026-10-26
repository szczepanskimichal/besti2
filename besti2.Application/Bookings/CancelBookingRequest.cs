namespace besti2.Application.Bookings;

// "Avlys" in the panel: an optional short message for the customer ("Beklager, Ingrid er syk");
// "Avbestill" in Min Besti: an optional reason for the business ("Jeg er syk")
public record CancelBookingRequest(string? Message);