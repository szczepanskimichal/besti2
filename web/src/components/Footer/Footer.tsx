import NextLink from "next/link";
import styles from "./Footer.module.css";

export default function Footer() {
    const year = new Date().getFullYear();

    return (
        <footer className={styles.footer}>
            {/* Two columns with Vilkår in the middle, like Ada's mockup */}
            <nav aria-label="Bunntekst" className={styles.links}>
                <NextLink href="/" className={styles.topLeft}>Finn bedrift</NextLink>
                <NextLink href="/aerlig-booking" className={styles.bottomLeft}>Ærlig booking</NextLink>
                <NextLink href="/vilkar" className={styles.middle}>Vilkår</NextLink>
                <NextLink href="/personvern" className={styles.topRight}>Personvern</NextLink>
                <NextLink href="/registrer-bedrift" className={styles.bottomRight}>Registrer bedrift</NextLink>
            </nav>
            <div className={styles.brand}>
                {/* eslint-disable-next-line @next/next/no-img-element -- small logo, next/image not needed */}
                <img src="/logo/besti-stopka-b.png" alt="" width={37} height={44} className={styles.logo}/>
                <div>
                    <p className={styles.slogan}>Bestill med din BESTI</p>
                    <p className={styles.copy}>© {year} BESTI</p>
                </div>
            </div>
        </footer>
    );
}
