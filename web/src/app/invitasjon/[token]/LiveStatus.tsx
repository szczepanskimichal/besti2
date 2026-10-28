"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";

const EVERY_MS = 4000;

// While the invitation waits, the page asks the server again every few seconds, so "Ola blir med!"
// appears by itself. Only while the tab is visible: a phone in the pocket costs nothing
export default function LiveStatus() {
    const router = useRouter();

    useEffect(() => {
        let timer: ReturnType<typeof setInterval> | undefined;
        const start = () => {
            if (timer === undefined) {
                timer = setInterval(() => router.refresh(), EVERY_MS);
            }
        };
        const stop = () => {
            clearInterval(timer);
            timer = undefined;
        };
        const onVisibility = () => {
            if (document.visibilityState === "visible") {
                router.refresh(); // back in the tab: show what happened at once
                start();
            } else {
                stop();
            }
        };
        if (document.visibilityState === "visible") {
            start();
        }
        document.addEventListener("visibilitychange", onVisibility);
        return () => {
            stop();
            document.removeEventListener("visibilitychange", onVisibility);
        };
    }, [router]);

    return null;
}
