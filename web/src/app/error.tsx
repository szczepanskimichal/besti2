"use client";
import styles from "./status.module.css";

type Props = {
    retry: () => void;
};
// Shown when a page crashes, e.g. the API is down
export default function ErrorPage({ retry }: Props) {
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Noe gikk galt</h1>
            <p className={styles.text}>Vi klarte ikke å laste siden. Prøv igjen om litt.</p>
            <button type="button" onClick={retry} className={styles.button}>
                Prøv igjen
            </button>
        </main>
    );
}