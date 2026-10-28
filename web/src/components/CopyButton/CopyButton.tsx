"use client";

import { useState } from "react";

// "Kopier": puts the text on the clipboard and says so for 2 seconds
export default function CopyButton({ text, label = "Kopier", className }: { text: string; label?: string; className?: string }) {
    const [copied, setCopied] = useState(false);

    async function copy() {
        try {
            await navigator.clipboard.writeText(text);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch {
            // Old browser or no permission: the text is still on the page to select by hand
        }
    }

    return (
        <button type="button" onClick={copy} className={className} aria-live="polite">
            {copied ? "Kopiert ✓" : label}
        </button>
    );
}
