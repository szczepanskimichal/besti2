"use client";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

// Asks the browser for the position. It is never stored: it only goes into the search URL,
// rounded to about 100 m. Used by the "Nær meg" button and by the "Nær meg" line in the city list
export function useNearMe() {
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

    return { status, findMe };
}
