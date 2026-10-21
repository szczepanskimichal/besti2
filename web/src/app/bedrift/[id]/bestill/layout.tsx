import type { Metadata } from "next";
import { privatePage } from "@/lib/site";

// Booking and its confirmation are steps for one customer, not pages for Google
export const metadata: Metadata = { title: "Bestill time", robots: privatePage };

export default function BookingLayout({ children }: { children: React.ReactNode }) {
    return children;
}
