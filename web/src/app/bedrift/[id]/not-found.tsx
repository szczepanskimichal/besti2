import NextLink from "next/link";
import styles from "./page.module.css";

export default function BusinessNotFound() {
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Fant ikke bedriften</h1>
            <p className={styles.muted}>Bedriften finnes ikke, eller er fjernet.</p>
            <NextLink href="/" className={styles.back}>← Tilbake til alle bedrifter</NextLink>
        </main>
    );
}