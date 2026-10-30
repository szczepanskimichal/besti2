import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
import {getMe, getUnreadCount} from "@/lib/api";
import {getToken} from "@/lib/auth";
import NotificationBell from "@/components/NotificationBell/NotificationBell";
import LogoutButton from "@/components/Header/LogoutButton";
import MobileMenu from "@/components/Header/MobileMenu";


// Server component: reads the login cookie and shows who is logged in
export default async function Header() {
    const token = await getToken();
    // If the token expired or the API is down, show the logged-out header instead of breaking every page
    const me = token ? await getMe(token).catch(() => null) : null;
    const firstName = me?.fullName?.split(" ")[0];
    // The number on the bell; no bell rather than a broken header when the API fails
    const unread = me && token ? await getUnreadCount(token).catch(() => null) : null;
    // Shown twice: in a row on a computer and in the ☰ menu on a phone
    const links = me && (me.isBusiness ? (
        <NextLink href="/panel" className={styles.ctaLink}>Panel</NextLink>
    ) : (
        <>
            {me.isAdmin && <NextLink href="/admin" className={styles.navLink}>Admin</NextLink>}
            {me.isStaff && <NextLink href="/ansatt" className={styles.navLink} title="Min arbeidsplan">Jobb</NextLink>}
            <NextLink href="/min-besti" className={styles.navLink}>Min Besti</NextLink>
        </>
    ));
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
                            {/* Computer: links in a row. Phone: the same links behind the ☰ button */}
                            <div className={styles.desktopLinks}>
                                {firstName && !me.isBusiness && <span className={styles.greeting}>Hei, {firstName}</span>}
                                {links}
                            </div>
                            {unread !== null && (
                                <NotificationBell key={unread} initialUnread={unread}/>
                            )}
                            <LogoutButton className={`${styles.logoutButton} ${styles.desktopOnly}`}/>
                            <MobileMenu>
                                {firstName && <span className={styles.menuGreeting}>Hei, {firstName}</span>}
                                {links}
                                <LogoutButton className={styles.logoutButton}/>
                            </MobileMenu>
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
