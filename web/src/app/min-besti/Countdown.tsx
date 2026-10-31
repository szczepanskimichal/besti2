"use client";
import { useSyncExternalStore } from "react";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import styles from "./Countdown.module.css";

// "Om 2 dager" on each coming visit. Seeing the time shrink makes people remember the visit or cancel in time
export default function Countdown({ startUtc }: { startUtc: string }) {
    // Known only in the browser (the server and the phone may disagree by a minute): nothing until then
    const now = useSyncExternalStore(subscribeToMinutes, currentMinute, () => null);
    if (now === null) {
        return null;
    }
    const left = new Date(startUtc).getTime() - now;
    return (
        <span className={`${styles.pill} ${left < 2 * hour ? styles.soon : ""}`}>
            <CrystalIcon name="stjerne"/> {left <= 0 ? "Nå" : `Om ${formatLeft(left)}`}
        </span>
    );
}

// "Gratis avbestilling i 1 dag og 8 t til", under Avbestill; gone when the time is up
export function FreeCancelLeft({ untilUtc }: { untilUtc: string | null }) {
    const now = useSyncExternalStore(subscribeToMinutes, currentMinute, () => null);
    const left = untilUtc && now !== null ? new Date(untilUtc).getTime() - now : 0;
    if (left <= 0) {
        return null;
    }
    return (
        <p className={styles.cancel}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" aria-hidden="true">
                <circle cx="12" cy="12" r="9"/><path d="M12 7v5l3.5 2"/>
            </svg>
            <span>Gratis avbestilling i <b>{formatLeft(left)}</b> til</span>
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
