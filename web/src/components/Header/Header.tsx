import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
import {getMe, getUnreadCount} from "@/lib/api";
import {getToken} from "@/lib/auth";
import NotificationBell from "@/components/NotificationBell/NotificationBell";
import LogoutButton from "@/components/Header/LogoutButton";


// Server component: reads the login cookie and shows who is logged in
export default async function Header() {
    const token = await getToken();
    // If the token expired or the API is down, show the logged-out header instead of breaking every page
    const me = token ? await getMe(token).catch(() => null) : null;
    const firstName = me?.fullName?.split(" ")[0];
    // The number on the bell; no bell rather than a broken header when the API fails
    const unread = me && token ? await getUnreadCount(token).catch(() => null) : null;
    return (
        <header className={styles.header}>
            <div className={styles.inner}>
                <NextLink href="/" className={styles.logo} aria-label="BESTI – til forsiden">
                    {/* eslint-disable-next-line @next/next/no-img-element -- small logo, next/image not needed */}
                    <img src="/logo/besti-napis.png" alt="" width={119} height={50} className={styles.logoImage}/>
                </NextLink>
                <nav className={styles.nav}>
                    {me ? (
                        <>
                            {me.isBusiness ? (
                                <NextLink href="/panel" className={styles.ctaLink}>Panel</NextLink>
                            ) : (
                                <>
                                    {firstName && <span className={styles.greeting}>Hei, {firstName}</span>}
                                    {me.isAdmin && <NextLink href="/admin" className={styles.navLink}>Admin</NextLink>}
                                    {/* Short, so it fits next to Min Besti on a phone */}
                                    {me.isStaff && <NextLink href="/ansatt" className={styles.navLink} title="Min arbeidsplan">Jobb</NextLink>}
                                    <NextLink href="/min-besti" className={styles.navLink}>Min
                                        Besti</NextLink>
                                </>
                            )}
                            {unread !== null && (
                                <NotificationBell key={unread} initialUnread={unread}/>
                            )}
                            <LogoutButton className={styles.logoutButton}/>
                        </>
                    ) : (
                        <>
                            <NextLink href="/logg-inn" className={styles.loginLink}>Logg inn</NextLink>
                            <NextLink href="/registrer-bedrift" className={styles.ctaLink}>Registrer bedrift</NextLink>
                        </>
                    )}
                </nav>
            </div>
        </header>
    );
}
