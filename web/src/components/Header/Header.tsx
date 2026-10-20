import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
import { getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { logoutAction } from "@/app/logg-inn/actions";


// Server component: reads the login cookie and shows who is logged in
export default async function Header() {
    const token = await getToken();
    // If the token expired or the API is down, show the logged-out header instead of breaking every page
    const me = token ? await getMe(token).catch(() => null) : null;
    const firstName = me?.fullName?.split(" ")[0];
    
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
                                    <NextLink href="/mine-bestillinger" className={styles.navLink}>Mine bestillinger</NextLink>
                                </>                            )}
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