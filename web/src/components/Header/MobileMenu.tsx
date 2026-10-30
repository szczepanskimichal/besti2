"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import styles from "@/components/Header/Header.module.css";

// On a phone the links and "Logg ut" do not fit next to the logo, so they live behind a ☰ button
export default function MobileMenu({ children }: { children: ReactNode }) {
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement>(null);

    // Close with Escape or a tap outside the menu
    useEffect(() => {
        if (!open) return;
        const onKey = (e: KeyboardEvent) => e.key === "Escape" && setOpen(false);
        const onClick = (e: MouseEvent) => {
            if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
        };
        document.addEventListener("keydown", onKey);
        document.addEventListener("click", onClick);
        return () => {
            document.removeEventListener("keydown", onKey);
            document.removeEventListener("click", onClick);
        };
    }, [open]);

    return (
        <div ref={rootRef} className={styles.mobileMenu}>
            <button
                type="button"
                className={styles.menuButton}
                aria-expanded={open}
                aria-controls="besti-menu"
                aria-label={open ? "Lukk meny" : "Åpne meny"}
                onClick={() => setOpen(!open)}
            >
                <span className={open ? `${styles.burger} ${styles.burgerOpen}` : styles.burger}/>
            </button>
            {open && (
                // The header stays on screen between pages, so a tap on a link closes the menu
                <div id="besti-menu" className={styles.menuPanel}
                     onClick={(e) => (e.target as HTMLElement).closest("a") && setOpen(false)}>
                    {children}
                </div>
            )}
        </div>
    );
}
