import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
import {getMe, getNotifications} from "@/lib/api";
import {getToken} from "@/lib/auth";
import {logoutAction} from "@/app/logg-inn/actions";
import NotificationBell from "@/components/NotificationBell/NotificationBell";


// Server component: reads the login cookie and shows who is logged in
export default async function Header() {
    const token = await getToken();
    // If the token expired or the API is down, show the logged-out header instead of breaking every page
    const me = token ? await getMe(token).catch(() => null) : null;
    const firstName = me?.fullName?.split(" ")[0];
    // The number on the bell; no bell rather than a broken header when the API fails
    const notifications = me && token ? await getNotifications(token).catch(() => null) : null;

    return (
        <header className={styles.header}>
            <div className={styles.inner}>
                <NextLink href="/" className={styles.logo}>BESTI</NextLink>
                <nav className={styles.nav}>
                    {me ? (
                        <>
                            {me.isBusiness ? (
                                <NextLink href="/panel" className={styles.ctaLink}>Panel</NextLink>
                            ) : (
                                <>
                                    {firstName && <span className={styles.greeting}>Hei, {firstName}</span>}
                                    <NextLink href="/min-besti" className={styles.navLink}>Min
                                        Besti</NextLink>
                                </>
                            )}
                            {notifications && (
                                <NotificationBell key={notifications.unread} initialUnread={notifications.unread}/>
                            )}
                            <form action={logoutAction}>
                                <button type="submit" className={styles.logoutButton}>Logg ut</button>
                            </form>
                        </>
                    ) : (
                        <>
                            <NextLink href="/logg-inn" className={styles.navLink}>Logg inn</NextLink>
                            <NextLink href="/registrer-bedrift" className={styles.ctaLink}>Registrer bedrift</NextLink>
                        </>
                    )}
                </nav>
            </div>
        </header>
    );
}