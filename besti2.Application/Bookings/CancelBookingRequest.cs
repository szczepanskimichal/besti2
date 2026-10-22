namespace besti2.Application.Bookings;

// "Avlys" in the panel: an optional short message for the customer ("Beklager, Ingrid er syk")
public record CancelBookingRequest(string? Message);