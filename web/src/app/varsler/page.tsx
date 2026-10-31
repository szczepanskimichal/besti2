import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getMe, getMyProfile, getNotifications, getPushPublicKey } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatAgo } from "@/lib/format";
import { privatePage } from "@/lib/site";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import { deleteNotificationAction, markAllReadAction } from "./actions";
import LineIcon, { type LineIconName } from "./LineIcon";
import PhoneApp from "./PhoneApp";
import ReminderSettings from "./ReminderSettings";
import SoundToggle from "./SoundToggle";
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
    const [notifications, pushKey, me] = await Promise.all([getNotifications(token), getPushPublicKey(), getMe(token)]);
    if (!notifications) {
        redirect("/logg-inn?neste=/varsler");
    }
    // The reminders are about the customer's own visits, so a business account does not get them
    const profile = me && !me.isBusiness ? await getMyProfile(token) : null;

    return (
        <main className={panelStyles.main}>
            <div className={styles.head}>
                <h1 className={styles.title}>Varsler</h1>
                {notifications.unread > 0 && (
                    <form action={markAllReadAction}>
                        <button type="submit" className={styles.markAll}>Merk alle som lest</button>
                    </form>
                )}
            </div>
            {notifications.items.length === 0 ? (
                <p className={panelStyles.muted}>Du har ingen varsler.</p>
            ) : (
                <ul className={panelStyles.list}>
                    {notifications.items.map((item) => {
                        const kind = kindOf(item.text);
                        return (
                        <li key={item.id} className={`${styles.item} ${item.isRead ? "" : styles.unread}`}>
                            <span className={styles.icon}>
                                {kind === "review" ? <CrystalIcon name="stjerne"/> : <LineIcon name={kind}/>}
                            </span>
                            {/* The whole card is the link. A plain link, not NextLink: prefetching would mark it as read before the click */}
                            <a href={`/varsler/${item.id}`} className={styles.body}>
                                <span className={styles.when}>
                                    {!item.isRead && <span className={styles.dot} aria-label="Ny"/>}
                                    {formatAgo(item.createdUtc)}
                                </span>
                                <span className={styles.text}>{item.text}</span>
                            </a>
                            <form action={deleteNotificationAction}>
                                <input type="hidden" name="id" value={item.id}/>
                                <button type="submit" className={styles.delete} aria-label="Slett varselet" title="Slett">
                                    <LineIcon name="bin"/>
                                </button>
                            </form>
                        </li>
                        );
                    })}
                </ul>
            )}
            {/* Settings at the bottom, closed: most visits are about the notifications themselves */}
            <details className={styles.settings}>
                <summary>
                    <span className={styles.icon}><LineIcon name="gear"/></span>
                    Innstillinger for varsler
                </summary>
                <div className={styles.settingsBody}>
                    <SoundToggle/>
                    {profile && <ReminderSettings reminderEmail={profile.reminderEmail} soonNotice={profile.soonNotice}/>}
                    <PhoneApp publicKey={pushKey}/>
                </div>
            </details>
        </main>
    );
}

// The icon from what the notification says (the API stores only the text)
function kindOf(text: string): LineIconName | "review" {
    if (/vurdering/i.test(text)) {
        return "review";
    }
    if (/bekreft/i.test(text)) {
        return "ok";
    }
    if (/ledig/i.test(text)) {
        return "calendar";
    }
    return "bell";
}
