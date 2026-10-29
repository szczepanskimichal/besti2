"use client";
import { useSyncExternalStore } from "react";
import styles from "./Countdown.module.css";

// "Om 2 t 15 min" on each coming visit, and how long free cancelling still lasts. Seeing the time shrink
// makes people remember the visit or cancel in time, instead of not showing up
export default function Countdown({ startUtc, freeCancelUntilUtc }: { startUtc: string; freeCancelUntilUtc: string | null }) {
    // Known only in the browser (the server and the phone may disagree by a minute): nothing until then
    const now = useSyncExternalStore(subscribeToMinutes, currentMinute, () => null);
    if (now === null) {
        return null;
    }
    const left = new Date(startUtc).getTime() - now;
    const cancelLeft = freeCancelUntilUtc ? new Date(freeCancelUntilUtc).getTime() - now : 0;
    return (
        <p className={styles.countdown}>
            <span className={`${styles.pill} ${left < 2 * hour ? styles.soon : ""}`}>
                <span aria-hidden="true">⏳</span> {left <= 0 ? "Nå" : `Om ${formatLeft(left)}`}
            </span>
            {cancelLeft > 0 && (
                <span className={styles.cancel}>Gratis avbestilling i {formatLeft(cancelLeft)} til</span>
            )}
        </p>
    );
}

const minute = 60 * 1000;
const hour = 60 * minute;
const day = 24 * hour;

// "3 dager", "1 dag og 4 t", "5 t 20 min", "12 min"
function formatLeft(ms: number): string {
    if (ms >= 2 * day) {
        return `${Math.floor(ms / day)} dager`;
    }
    if (ms >= day) {
        const hours = Math.floor((ms - day) / hour);
        return hours > 0 ? `1 dag og ${hours} t` : "1 dag";
    }
    const total = Math.ceil(ms / minute);
    const hours = Math.floor(total / 60);
    const minutes = total % 60;
    if (hours === 0) {
        return `${minutes} min`;
    }
    return minutes > 0 ? `${hours} t ${minutes} min` : `${hours} t`;
}

// One clock for all the cards, rounded to the minute so React only redraws once a minute
function currentMinute(): number {
    return Math.floor(Date.now() / minute) * minute;
}

function subscribeToMinutes(onChange: () => void) {
    const timer = setInterval(onChange, 15 * 1000);
    return () => clearInterval(timer);
}
