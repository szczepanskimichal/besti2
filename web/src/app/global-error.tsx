"use client";
import "./globals.css";
import styles from "./status.module.css";

type Props = {
    retry: () => void;
};
// Last safety net: shown when even the root layout (header/footer) crashes.
// It replaces the whole layout, so it needs its own html and body.
export default function GlobalError({ retry }: Props) {
    return (
        <html lang="nb">
        <head>
            <title>Noe gikk galt | BESTI</title>
        </head>
        <body>
        <main className={styles.main}>
            <h1 className={styles.title}>Noe gikk galt</h1>
            <p className={styles.text}>Vi klarte ikke å laste siden. Prøv igjen om litt.</p>
            <button type="button" onClick={retry} className={styles.button}>
                Prøv igjen
            </button>
        </main>
        </body>
        </html>
    );
}
