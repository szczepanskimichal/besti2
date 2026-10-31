"use client";
import NextLink from "next/link";
import { usePathname } from "next/navigation";

// "Logg inn" in the header, hidden on the pages that already lead to logging in
const HIDDEN_ON = ["/logg-inn", "/registrer", "/glemt-passord"];

export default function LoginLink({ className }: { className: string }) {
    if (HIDDEN_ON.includes(usePathname())) {
        return null;
    }
    return <NextLink href="/logg-inn" className={className}>Logg inn</NextLink>;
}
