import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { getNotifications, markNotificationsRead } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime } from "@/lib/format";
import { privatePage } from "@/lib/site";
// Same cards as Min Besti and the panel
import panelStyles from "../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Varsler",
    robots: privatePage,
};

// The list behind the bell, for customers and businesses. It shows what is new once: opening it clears the list
export default async function NotificationsPage() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/varsler");
    }
    const notifications = await getNotifications(token);
    if (!notifications) {
        redirect("/logg-inn?neste=/varsler");
    }
    if (notifications.unread > 0) {
        await markNotificationsRead(token);
    }

    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Varsler</h1>
            {notifications.items.length === 0 ? (
                <p className={panelStyles.muted}>Ingen nye varsler.</p>
            ) : (
                <ul className={panelStyles.list}>
                    {notifications.items.map((item) => (
                        <li key={item.id} className={`${panelStyles.card} ${item.isRead ? "" : styles.unread}`}>
                            <p className={styles.when}>
                                {formatDate(item.createdUtc)} kl. {formatTime(item.createdUtc)}
                                {!item.isRead && <span className={styles.newLabel}>Ny</span>}
                            </p>
                            {item.link ? <Link href={item.link}>{item.text}</Link> : <p>{item.text}</p>}
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
