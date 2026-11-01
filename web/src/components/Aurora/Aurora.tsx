import styles from "./Aurora.module.css";

// The northern lights behind every page (2026-11-01): one calm, still photo by Tomáš Malík / Unsplash,
// made from the original 6000 px photo. No moving layers, so it costs nothing while scrolling.
export default function Aurora() {
    return (
        <div className={styles.sky} aria-hidden="true">
            <i className={styles.photo}/>
            <div className={styles.shade}/>
        </div>
    );
}
