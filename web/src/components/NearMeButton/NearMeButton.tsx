"use client";
import { type CSSProperties } from "react";
import styles from "./NearMeButton.module.css";
import { useNearMe } from "./useNearMe";

// The "Nær meg" button under the search (the same as the "Nær meg" line in the city list)
export default function NearMeButton() {
    const { status, findMe } = useNearMe();

    return (
        <div className={styles.wrapper}>
            <button type="button" onClick={findMe} className={styles.button} disabled={status === "finding"}>
                {/* Ada's crystal pin: a soft twinkle now and then, and a pop with sparks when tapped */}
                <span className={status === "finding" ? `${styles.pin} ${styles.finding}` : styles.pin} aria-hidden="true">
                    {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
                    <img src="/ikoner/pinne.svg" alt="" width={40} height={40}/>
                    <span className={styles.glint}/>
                    {status === "finding" && (
                        <span className={styles.sparks}>
                            {[0, 1, 2, 3, 4, 5].map((k) => (
                                <i key={k} style={{ "--a": `${k * 60 - 60}deg` } as CSSProperties}>{k % 2 ? "✦" : "✧"}</i>
                            ))}
                        </span>
                    )}
                </span>
                {status === "finding" ? "Finner deg …" : "Nær meg"}
            </button>
            {status === "denied" && (
                <p className={styles.denied} role="alert">
                    Fikk ikke tilgang til posisjonen din. Skriv inn en by i stedet.
                </p>
            )}
        </div>
    );
}