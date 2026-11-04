import styles from "./FreeCancelPill.module.css";

// "Gratis avbestilling inntil 24 timer før timen" as the same dark blue pill with a clock as in Min BESTI
// ("Gratis avbestilling i 1 dag til"), so free cancelling looks the same everywhere (Ada 2026-11-03)
export default function FreeCancelPill({ hours }: { hours: number }) {
    return (
        <p className={styles.pill}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" aria-hidden="true">
                <circle cx="12" cy="12" r="9"/><path d="M12 7v5l3.5 2"/>
            </svg>
            {hours === 0
                ? <span>Gratis avbestilling <b>når som helst</b> før timen</span>
                : <span>Gratis avbestilling inntil <b>{hours} timer</b> før timen</span>}
        </p>
    );
}
