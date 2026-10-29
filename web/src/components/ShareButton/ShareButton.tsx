"use client";

import { useState } from "react";

type ShareButtonProps = {
    path: string; // e.g. "/klipp-og-stil-frisor-gjovik"
    title: string;
    text: string;
    label?: string;
    className?: string;
};

// "Del": the phone's own share sheet (Messenger, SMS, Snapchat …); on a computer the link is copied instead
export default function ShareButton({ path, title, text, label = "Del", className }: ShareButtonProps) {
    const [copied, setCopied] = useState(false);

    async function share() {
        const url = new URL(path, window.location.origin).toString();
        if (navigator.share) {
            try {
                await navigator.share({ title, text, url });
            } catch {
                // Closed the share sheet: nothing to do
            }
            return;
        }
        try {
            await navigator.clipboard.writeText(url);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch {
            // Old browser or no permission: the address bar still has the link
        }
    }

    return (
        <button type="button" onClick={share} className={className} aria-live="polite">
            <span aria-hidden="true">↗</span> {copied ? "Lenke kopiert ✓" : label}
        </button>
    );
}
