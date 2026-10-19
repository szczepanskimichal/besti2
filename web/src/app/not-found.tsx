import Link from "next/link";
import styles from "./status.module.css";
// Shown for unknown addresses and when a page calls notFound()
export default function NotFound() {
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Fant ikke siden</h1>
            <p className={styles.text}>Siden du leter etter finnes ikke, eller den er flyttet.</p>
            <Link href="/" className={styles.link}>
                Til forsiden
            </Link>
        </main>
    );
}