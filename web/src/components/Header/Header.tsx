import NextLink from "next/link";
import styles from "@/components/Header/Header.module.css";
export default function Header() {
    return (
        <header className={styles.header}>
            <div className={styles.inner}>
                <NextLink href="/" className={styles.logo}>BESTI</NextLink>
                <nav className={styles.nav}>
                    <NextLink href="/panel" className={styles.navLink}>For bedrifter</NextLink>
                </nav>
            </div>    
        </header>
    );
}