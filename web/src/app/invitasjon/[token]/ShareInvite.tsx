"use client";
import { useState, useSyncExternalStore, type ReactNode } from "react";
import CopyButton from "@/components/CopyButton/CopyButton";
import styles from "./page.module.css";

// "Del invitasjon": the phone's own share sheet (WhatsApp, Snap, Messenger …) where it exists,
// otherwise WhatsApp, Snapchat, SMS and "Kopier lenke" (a computer usually has no share sheet)
// children ("Du venter på svar …") show up once the link is sent with any button; the browser remembers it,
// so it is still there when the inviter comes back to the page
export default function ShareInvite({ url, text, children }: { url: string; text: string; children?: ReactNode }) {
    // Known only in the browser (the server renders the fallback buttons first)
    const canShare = useSyncExternalStore(noChanges, () => typeof navigator.share === "function", () => false);
    const [shared, setShared] = useState(false);
    const sentBefore = useSyncExternalStore(noChanges, () => readSent(url), () => false);
    const [sentNow, setSentNow] = useState(false);
    const sent = sentBefore || sentNow;

    function markSent() {
        setSentNow(true);
        try {
            localStorage.setItem(sentKey(url), "1");
        } catch {
            // Private mode: it just shows for this visit
        }
    }

    async function share() {
        try {
            await navigator.share({ title: "Bli med meg? 👯", text, url });
            setShared(true);
            markSent();
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
            {/* A click on WhatsApp, Snapchat, SMS or Kopier counts as sent */}
            <div className={styles.shareRow} onClickCapture={markSent}>
                <a href={`https://wa.me/?text=${encodeURIComponent(message)}`} className={styles.secondaryButton}
                   target="_blank" rel="noopener noreferrer">WhatsApp</a>
                {/* Many Norwegians use Snap: opens Snapchat with the link attached */}
                <a href={`https://www.snapchat.com/scan?attachmentUrl=${encodeURIComponent(url)}`} className={styles.secondaryButton}
                   target="_blank" rel="noopener noreferrer">Snapchat</a>
                <a href={`sms:?&body=${encodeURIComponent(message)}`} className={styles.secondaryButton}>SMS</a>
                <CopyButton text={url} label="Kopier" className={styles.secondaryButton}/>
            </div>
            <p className={styles.link}>{url}</p>
            {sent && <div className={styles.afterSend}>{children}</div>}
        </div>
    );
}

function sentKey(url: string) {
    return `besti-invite-sent:${url}`;
}

function readSent(url: string): boolean {
    try {
        return localStorage.getItem(sentKey(url)) === "1";
    } catch {
        return false;
    }
}

// Whether the browser can share never changes while the page is open
function noChanges() {
    return () => {};
}
