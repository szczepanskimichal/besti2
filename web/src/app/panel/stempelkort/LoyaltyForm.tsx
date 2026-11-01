import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { saveLoyaltyAction } from "./actions";
import styles from "./page.module.css";

const options = [5, 6, 7, 8, 9, 10];

// How many visits fill the card (round glass tiles, the chosen one lit mint) and what the customer gets.
// The reward field, Lagre endringer and Slå av sit side by side, one size each
export default function LoyaltyForm({ visits, reward }: { visits: number | null; reward: string | null }) {
    const chosen = visits ?? 10;
    return (
        <form action={saveLoyaltyAction} className={styles.form}>
            <fieldset className={styles.visits}>
                <legend className={styles.question}>Antall besøk for en belønning</legend>
                <div className={styles.visitChoices}>
                    {options.map((n) => (
                        <label key={n} className={styles.visitChoice}>
                            <input type="radio" name="visits" value={n} defaultChecked={n === chosen}/>
                            <span className={styles.visitNumber}>{n}</span>
                            <span className={styles.visitText}>besøk</span>
                        </label>
                    ))}
                </div>
            </fieldset>
            <label className={styles.field} htmlFor="loyalty-reward">
                Belønning
                <span id="loyalty-reward-hint" className={styles.fieldHint}>Skriv inn din egen belønning eller tjeneste</span>
            </label>
            <div className={styles.rewardRow}>
                <input id="loyalty-reward" name="reward" aria-describedby="loyalty-reward-hint" required maxLength={100} defaultValue={reward ?? ""}
                       className={styles.rewardInput}
                       placeholder="F.eks. «Gratis herreklipp» eller «50 % på neste behandling»"/>
                <SubmitButton className={styles.saveButton} pendingText="Lagrer …">
                    {visits ? "Lagre endringer" : "Slå på stempelkort"}
                </SubmitButton>
                {visits && (
                    // Turning off is rare and resets every card, so the warning shows only after a click.
                    // Same form: the "off" button skips the required check on the reward field
                    <details className={styles.more}>
                        <summary className={`${styles.pill} ${styles.pillDanger}`}>Slå av stempelkort</summary>
                        <div className={styles.sheet}>
                            <p className={styles.text}>Slår du det på igjen senere, starter alle kundene på null.</p>
                            <button type="submit" name="off" value="1" formNoValidate className={styles.dangerButton}>
                                Ja, slå av
                            </button>
                        </div>
                    </details>
                )}
            </div>
        </form>
    );
}
