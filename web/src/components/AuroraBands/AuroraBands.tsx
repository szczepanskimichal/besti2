import styles from "./AuroraBands.module.css";

// Moving aurora behind the home page; purely decorative, so hidden from screen readers
export default function AuroraBands() {
    return (
        <div className={styles.sky} aria-hidden="true">
            <div className={`${styles.band} ${styles.b1}`} />
            <div className={`${styles.band} ${styles.b2}`} />
            <div className={`${styles.band} ${styles.b3}`} />
        </div>
    );
}