import type { Metadata } from "next";
import { privatePage } from "@/lib/site";

export const metadata: Metadata = { title: "Glemt passord", robots: privatePage };

export default function ForgotPasswordLayout({ children }: { children: React.ReactNode }) {
    return children;
}