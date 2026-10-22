"use client";
import { useEffect, useState } from "react";
import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";

// The bell in the header. The number changes at the moment something happens: the server sends it over an open
// connection (Server-Sent Events, /varsler/stream) – a new booking, a cancellation, an answer, a review, or
// reading / deleting in another tab. The browser reconnects by itself if the connection drops
export default function NotificationBell({ initialUnread }: { initialUnread: number }) {
    const [unread, setUnread] = useState(initialUnread);

    useEffect(() => {
        const stream = new EventSource("/varsler/stream");
        stream.onmessage = (event) => {
            const count = Number(event.data);
            if (Number.isFinite(count)) {
                setUnread(count);
            }
        };
        return () => stream.close();
    }, []);

    return (
        <NextLink href="/varsler" className={styles.bell}
                  aria-label={unread > 0 ? `Varsler, ${unread} uleste` : "Varsler"}>
            <span aria-hidden="true">🔔</span>
            {unread > 0 && (
                <span className={styles.badge} aria-hidden="true">{unread > 9 ? "9+" : unread}</span>
            )}
        </NextLink>
    );
}