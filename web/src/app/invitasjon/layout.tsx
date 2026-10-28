import type { Metadata } from "next";
import { privatePage } from "@/lib/site";

// An invitation belongs to two people: never in Google
export const metadata: Metadata = { title: "Invitasjon", robots: privatePage };

export default function InviteLayout({ children }: { children: React.ReactNode }) {
    return children;
}
