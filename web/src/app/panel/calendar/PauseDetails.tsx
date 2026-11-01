import Link from "next/link";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";

// Ada 2026-11-01: the break stays closed until the business itself decides to work in it.
// A click on "Pause" in the grid asks first: add a visit yourself in the break, or keep it free.
// (Opening the break for online booking for one day needs backend work: a later, separate change)
export default function PauseDetails({ when, hours, who, newHref, closeHref }: {
    when: string; hours: string; who: string; newHref: string; closeHref: string;
}) {
    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="pause-title">
                <div className={calendar.dialogHead}>
                    <h2 id="pause-title" className={calendar.dialogTitle}>Pause {hours}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogWhen}>{when}</p>
                <p className={styles.service}>{who}</p>
                <div className={calendar.pauseStripe} aria-hidden="true"/>
                <p className={calendar.pauseQuestion}>Vil du jobbe i pausen?</p>
                <p className={calendar.pauseText}>
                    Kundene kan ikke bestille i pausen, men du kan legge inn en time selv.
                </p>
                <div className={calendar.pauseChoices}>
                    <Link href={newHref} scroll={false} className={styles.primaryButton}>Legg inn time selv</Link>
                </div>
                <Link href={closeHref} scroll={false} className={calendar.pauseKeep}>Nei, behold pausen</Link>
            </section>
        </div>
    );
}
