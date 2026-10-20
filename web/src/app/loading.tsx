import styles from "./loading.module.css";

// Shown while any page waits for its data
export default function Loading() {
    return (
        <main className={styles.main} aria-busy="true" aria-label="Laster">
            <div className={styles.title}/>
            <div className={styles.block}/>
            <div className={styles.block}/>
        </main>
    );
}