"use client";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import styles from "./NearMeButton.module.css";

// Asks the browser for the position. It is never stored: it only goes into the search URL,
// rounded to about 100 m
export default function NearMeButton() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const [status, setStatus] = useState<"idle" | "finding" | "denied">("idle");

    function findMe() {
        if (!navigator.geolocation) {
            setStatus("denied");
            return;
        }
        setStatus("finding");
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const params = new URLSearchParams(searchParams.toString());
                params.set("lat", position.coords.latitude.toFixed(3));
                params.set("lng", position.coords.longitude.toFixed(3));
                params.delete("city"); // near me replaces the city
                params.delete("page"); // a new list starts on page 1
                setStatus("idle");
                router.push(`/?${params}`);
            },
            () => setStatus("denied"),
            { timeout: 10000 },
        );
    }

    return (
        <div className={styles.wrapper}>
            <button type="button" onClick={findMe} className={styles.button} disabled={status === "finding"}>
                {status === "finding" ? "Finner deg …" : (
                    <>
                        {/* Neon map pin, same green to turquoise as the button outlines */}
                        <svg className={styles.pin} viewBox="0 0 24 30" width="20" height="25" aria-hidden="true">
                            <defs>
                                <linearGradient id="besti-pin-gradient" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0" stopColor="#5af05e"/>
                                    <stop offset="1" stopColor="#28c9e8"/>
                                </linearGradient>
                            </defs>
                            <path d="M12 2.2c-4.9 0-8.8 3.8-8.8 8.6 0 5.9 6.5 12.1 8 14.1.4.5 1.2.5 1.6 0 1.5-2 8-8.2 8-14.1 0-4.8-3.9-8.6-8.8-8.6z"
                                  fill="none" stroke="url(#besti-pin-gradient)" strokeWidth="2.2" strokeLinejoin="round"/>
                            <circle cx="12" cy="10.8" r="3.4" fill="none" stroke="url(#besti-pin-gradient)" strokeWidth="2.2"/>
                        </svg>
                        Nær meg
                    </>
                )}
            </button>
            {status === "denied" && (
                <p className={styles.denied} role="alert">
                    Fikk ikke tilgang til posisjonen din. Skriv inn en by i stedet.
                </p>
            )}
        </div>
    );
}