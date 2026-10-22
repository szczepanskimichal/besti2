import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getNotifications } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatTime } from "@/lib/format";
import { privatePage } from "@/lib/site";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { deleteNotificationAction, markAllReadAction } from "./actions";
// Same cards as Min Besti and the panel
import panelStyles from "../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Varsler",
    robots: privatePage,
};

// The list behind the bell, for customers and businesses. A click opens what the notification is about and
// marks only that one as read; the bin deletes one. Old ones go by themselves (7 days customer, 14 days business)
export default async function NotificationsPage() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/varsler");
    }
    const notifications = await getNotifications(token);
    if (!notifications) {
        redirect("/logg-inn?neste=/varsler");
    }

    return (
        <main className={panelStyles.main}>
            <div className={styles.head}>
                <h1 className={panelStyles.title}>Varsler</h1>
                {notifications.unread > 0 && (
                    <form action={markAllReadAction}>
                        <SubmitButton className={panelStyles.secondaryButton} pendingText="Lagrer…">
                            Merk alle som lest
                        </SubmitButton>
                    </form>
                )}
            </div>
            {notifications.items.length === 0 ? (
                <p className={panelStyles.muted}>Du har ingen varsler.</p>
            ) : (
                <ul className={panelStyles.list}>
                    {notifications.items.map((item) => (
                        <li key={item.id} className={`${panelStyles.card} ${styles.item} ${item.isRead ? "" : styles.unread}`}>
                            <div className={styles.body}>
                                <p className={styles.when}>
                                    {formatDate(item.createdUtc)} kl. {formatTime(item.createdUtc)}
                                    {!item.isRead && <span className={styles.newLabel}>Ny</span>}
                                </p>
                                {/* A plain link, not NextLink: prefetching would mark it as read before the click */}
                                <a href={`/varsler/${item.id}`}>{item.text}</a>
                            </div>
                            <form action={deleteNotificationAction}>
                                <input type="hidden" name="id" value={item.id}/>
                                <button type="submit" className={styles.delete} aria-label="Slett varselet" title="Slett">
                                    🗑️
                                </button>
                            </form>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}
