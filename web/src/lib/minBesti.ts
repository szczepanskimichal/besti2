import type { CustomerBooking } from "@/lib/types";

// Same window as the API (ReviewService.ReviewDays): a visit can be reviewed for 30 days
const REVIEW_DAYS = 30;
const DAY_MS = 24 * 60 * 60 * 1000;

// Min Besti shows only what still matters: visits that have not started and are not cancelled, soonest first
export function upcomingBookings(bookings: CustomerBooking[]): CustomerBooking[] {
    const now = Date.now();
    return bookings
        .filter((b) => b.status !== "Avlyst" && new Date(b.startUtc).getTime() > now)
        .sort((a, b) => a.startUtc.localeCompare(b.startUtc));
}

// Visits that are over, not cancelled, not reviewed yet and at most 30 days old; newest first
export function bookingsToReview(bookings: CustomerBooking[]): CustomerBooking[] {
    const now = Date.now();
    return bookings
        .filter((b) => {
            const end = new Date(b.endUtc).getTime();
            return b.status !== "Avlyst" && !b.isReviewed && end <= now && end > now - REVIEW_DAYS * DAY_MS;
        })
        .sort((a, b) => b.startUtc.localeCompare(a.startUtc));
}