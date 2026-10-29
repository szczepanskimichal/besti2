import Link from "next/link";
import styles from "./page.module.css";

// The top bar of the panel: title, Kalender / Liste / Statistikk and the settings pages
export default function PanelNav({ title }: { title: string }) {
    return (
        <div className={styles.topBar}>
            <h1 className={styles.title}>{title}</h1>
            <nav className={styles.topActions} aria-label="Panel">
                <Link href="/panel" className={styles.secondaryButton}>Kalender</Link>
                <Link href="/panel/liste" className={styles.secondaryButton}>Liste</Link>
                <Link href="/panel/statistikk" className={styles.secondaryButton}>Statistikk</Link>
                <Link href="/panel/tjenester" className={styles.secondaryButton}>Tjenester</Link>
                <Link href="/panel/ansatte" className={styles.secondaryButton}>Ansatte</Link>
                <Link href="/panel/profil" className={styles.secondaryButton}>Profil</Link>
                <Link href="/panel/galleri" className={styles.secondaryButton}>Galleri</Link>
                <Link href="/panel/vurderinger" className={styles.secondaryButton}>Vurderinger</Link>
                <Link href="/panel/stempelkort" className={styles.secondaryButton}>Stempelkort</Link>
                <Link href="/panel/apningstider" className={styles.secondaryButton}>Åpningstider</Link>
                <Link href="/panel/del" className={styles.secondaryButton}>Del og få kunder</Link>
            </nav>
        </div>
    );
}