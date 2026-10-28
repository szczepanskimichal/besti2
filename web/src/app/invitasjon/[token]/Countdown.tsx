"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import styles from "./page.module.css";

// "Svar innen 27:13": ticks every second; at 0 the page asks the server again (the invitation runs out)
export default function Countdown({ expiresUtc, className }: { expiresUtc: string; className?: string }) {
    const router = useRouter();
    const end = new Date(expiresUtc).getTime();
    // null until the browser has started: the server's clock is not the visitor's, so no time is guessed there
    const [left, setLeft] = useState<number | null>(null);

    useEffect(() => {
        const tick = () => {
            const ms = end - Date.now();
            setLeft(Math.max(ms, 0));
            if (ms <= 0) {
                clearInterval(timer);
                router.refresh();
            }
        };
        const timer = setInterval(tick, 1000);
        tick();
        return () => clearInterval(timer);
    }, [end, router]);

    if (left === null) {
        return <span className={className}>…</span>;
    }
    const totalSeconds = Math.ceil(left / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    // Screen readers get whole minutes only (a new number every second would never stop talking)
    return (
        <span className={className}>
            <span aria-hidden="true">{minutes}:{String(seconds).padStart(2, "0")}</span>
            <span className={styles.srOnly}>{minutes < 1 ? "under ett minutt" : `${minutes} minutter`}</span>
        </span>
    );
}
