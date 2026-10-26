"use client";
import type { ReactNode } from "react";

// "Nei, behold timen": just closes the <details> around it, nothing is sent
export default function CloseDetailsButton({ className, children }: { className?: string; children: ReactNode }) {
    return (
        <button type="button" className={className}
                onClick={(event) => event.currentTarget.closest("details")?.removeAttribute("open")}>
            {children}
        </button>
    );
}