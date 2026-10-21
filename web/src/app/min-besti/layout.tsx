import type { Metadata } from "next";
import { privatePage } from "@/lib/site";

// Min Besti belongs to one customer: never in search engines
export const metadata: Metadata = { robots: privatePage };

export default function MinBestiLayout({ children }: { children: React.ReactNode }) {
    return children;
}