"use client";
import { useEffect, useRef, useState } from "react";
import NextLink from "next/link";
import { playNotificationSound, unlockSoundOnFirstTap } from "@/lib/notificationSound";
import { startPhoneApp } from "@/lib/phoneApp";
import styles from "@/components/Header/Header.module.css";

// The bell in the header. The number changes at the moment something happens: the server sends it over an open
// connection (Server-Sent Events, /varsler/stream) – a new booking, a cancellation, an answer, a review, or
// reading / deleting in another tab. The browser reconnects by itself if the connection drops.
// When the number goes UP, the bell rings: a short sound (can be turned off under Varsler) and a small swing
export default function NotificationBell({ initialUnread }: { initialUnread: number }) {
    const [unread, setUnread] = useState(initialUnread);
    const [ringing, setRinging] = useState(false);
    const lastCount = useRef(initialUnread);

    useEffect(() => {
        // The service worker for push messages and "Installer BESTI" (see phoneApp)
        startPhoneApp();
        const stopUnlock = unlockSoundOnFirstTap();
        const stream = new EventSource("/varsler/stream");
        stream.onmessage = (event) => {
            const count = Number(event.data);
            if (!Number.isFinite(count)) {
                return;
            }
            // Fewer (read or deleted) or the same after a reconnect: no sound
            if (count > lastCount.current) {
                playNotificationSound();
                setRinging(true);
            }
            lastCount.current = count;
            setUnread(count);
        };
        return () => {
            stream.close();
            stopUnlock();
        };
    }, []);

    // The number also in the tab's title, "(2) Min Besti | BESTI", so it shows among many open tabs.
    // Next.js writes a new title on every page change, so it is put back each time the title changes
    useEffect(() => {
        const apply = () => {
            const plain = document.title.replace(/^\(\d+\+?\) /, "");
            const wanted = unread > 0 ? `(${unread > 9 ? "9+" : unread}) ${plain}` : plain;
            if (document.title !== wanted) {
                document.title = wanted;
            }
        };
        apply();
        const observer = new MutationObserver(apply);
        observer.observe(document.head, { subtree: true, childList: true, characterData: true });
        // BESTI added to the home screen: the number on the app icon too (does nothing elsewhere)
        (unread > 0 ? navigator.setAppBadge?.(unread) : navigator.clearAppBadge?.())?.catch(() => {});
        return () => observer.disconnect();
    }, [unread]);

    return (
        <NextLink href="/varsler" className={styles.bell}
                  aria-label={unread > 0 ? `Varsler, ${unread} uleste` : "Varsler"}>
            <span aria-hidden="true" className={ringing ? styles.ringing : undefined}
                  onAnimationEnd={() => setRinging(false)}>🔔</span>
            {unread > 0 && (
                <span className={styles.badge} aria-hidden="true">{unread > 9 ? "9+" : unread}</span>
            )}
        </NextLink>
    );
}
