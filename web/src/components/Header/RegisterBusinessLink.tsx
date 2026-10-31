"use client";
import NextLink from "next/link";
import { usePathname } from "next/navigation";

// "Registrer bedrift" in the header, hidden on the sign-up page itself
export default function RegisterBusinessLink({ className }: { className: string }) {
    if (usePathname() === "/registrer-bedrift") {
        return null;
    }
    return <NextLink href="/registrer-bedrift" className={className}>Registrer bedrift</NextLink>;
}
