import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
export default function Header() {
    return (
        <header className={styles.header}>
            <div className={styles.inner}>
                <NextLink href="/" className={styles.logo}>BESTI</NextLink>
                <nav className={styles.nav}>
                    <NextLink href="/logg-inn" className={styles.navLink}>Logg inn</NextLink>
                    <NextLink href="/registrer-bedrift" className={styles.ctaLink}>Registrer bedrift</NextLink>
                </nav>
            </div>    
        </header>
    );
}