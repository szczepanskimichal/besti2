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
                {status === "finding" ? "Finner deg …" : "📍 Nær meg"}
            </button>
            {status === "denied" && (
                <p className={styles.denied} role="alert">
                    Fikk ikke tilgang til posisjonen din. Skriv inn en by i stedet.
                </p>
            )}
        </div>
    );
}