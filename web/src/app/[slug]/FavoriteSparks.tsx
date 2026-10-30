"use client";

import { useEffect, useRef, useState, type CSSProperties } from "react";
import styles from "./page.module.css";

const KEY = "besti-favoritt-gnister";

// A small burst of aurora sparks around the heart, only right after "Lagre" was pressed
// (not when a page with an already saved favorite is opened). The page is drawn again after saving,
// so the press is remembered for that one moment in sessionStorage
export default function FavoriteSparks({ businessId, saved }: { businessId: string; saved: boolean }) {
    const ref = useRef<HTMLSpanElement>(null);
    const [burst, setBurst] = useState(false);

    useEffect(() => {
        const form = ref.current?.closest("form");
        const remember = () => {
            try {
                if (!saved) sessionStorage.setItem(KEY, businessId);
            } catch {
                // no storage (private window): just no sparks
            }
        };
        form?.addEventListener("submit", remember);
        return () => form?.removeEventListener("submit", remember);
    }, [businessId, saved]);

    useEffect(() => {
        try {
            if (saved && sessionStorage.getItem(KEY) === businessId) {
                sessionStorage.removeItem(KEY);
                // eslint-disable-next-line react-hooks/set-state-in-effect -- sessionStorage only exists in the browser
                setBurst(true);
            }
        } catch {
            // no storage: no sparks
        }
    }, [businessId, saved]);

    return (
        <span ref={ref} className={styles.sparks} aria-hidden="true">
            {burst && saved && [0, 1, 2, 3, 4, 5, 6, 7].map((i) => (
                <i key={i} style={{ "--a": `${i * 45}deg` } as CSSProperties}>✦</i>
            ))}
        </span>
    );
}
