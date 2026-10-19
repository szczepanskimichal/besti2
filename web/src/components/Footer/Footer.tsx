import NextLink from "next/link";
import styles from "./Footer.module.css";

export default function Footer() {
    const year = new Date().getFullYear();

    return (
        <footer className={styles.footer}>
            <div className={styles.inner}>
                <div>
                    <p className={styles.brand}>BESTI</p>
                    <p className={styles.muted}>Finn og bestill time hos lokale bedrifter.</p>
                </div>
                <nav aria-label="Bunntekst" className={styles.links}>
                    <NextLink href="/">Finn bedrift</NextLink>
                    <NextLink href="/registrer-bedrift">Registrer bedrift</NextLink>
                    <NextLink href="/panel">Logg inn</NextLink>
                    <NextLink href="/personvern">Personvern</NextLink>
                    <NextLink href="/vilkar">Vilkår</NextLink>
                </nav>
            </div>
            <p className={styles.copy}>© {year} BESTI</p>
        </footer>
    );
}