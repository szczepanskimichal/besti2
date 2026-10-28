"use client";
import { useState, useSyncExternalStore } from "react";
import CopyButton from "@/components/CopyButton/CopyButton";
import styles from "./page.module.css";

// "Del invitasjon": the phone's own share sheet (WhatsApp, Snap, Messenger …) where it exists,
// otherwise WhatsApp, SMS and "Kopier lenke" (a computer usually has no share sheet)
export default function ShareInvite({ url, text }: { url: string; text: string }) {
    // Known only in the browser (the server renders the fallback buttons first)
    const canShare = useSyncExternalStore(noChanges, () => typeof navigator.share === "function", () => false);
    const [shared, setShared] = useState(false);

    async function share() {
        try {
            await navigator.share({ title: "Bli med meg? 👯", text, url });
            setShared(true);
        } catch {
            // Closed the share sheet: nothing to do
        }
    }

    const message = `${text} ${url}`;
    return (
        <div className={styles.share}>
            {canShare && (
                <button type="button" onClick={share} className={styles.primaryButton}>
                    {shared ? "Del igjen" : "Del invitasjon"} <span aria-hidden="true">📲</span>
                </button>
            )}
            <div className={styles.shareRow}>
                <a href={`https://wa.me/?text=${encodeURIComponent(message)}`} className={styles.secondaryButton}
                   target="_blank" rel="noopener noreferrer">WhatsApp</a>
                <a href={`sms:?&body=${encodeURIComponent(message)}`} className={styles.secondaryButton}>SMS</a>
                <CopyButton text={url} label="Kopier" className={styles.secondaryButton}/>
            </div>
            <p className={styles.link}>{url}</p>
        </div>
    );
}

// Whether the browser can share never changes while the page is open
function noChanges() {
    return () => {};
}
