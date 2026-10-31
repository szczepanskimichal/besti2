import Welcome from "@/components/Welcome/Welcome";
import styles from "@/components/Welcome/Welcome.module.css";
import { welcomeSeenAction } from "./actions";

// "Velkommen til BESTI": shown once to a new business on its first visit to the panel
export default function BusinessWelcome({ name, waiting }: { name: string; waiting: boolean }) {
    return (
        <Welcome title={<>Velkommen til BESTI,<br/>{name}!</>}
                 text={waiting
                     ? "Vi ser over bedriften din. Imens kan du legge inn tjenester og åpningstider."
                     : "Legg inn tjenester og åpningstider, så kan kundene bestille."}>
            <form action={welcomeSeenAction}>
                <button type="submit" className={styles.go}>Sett opp bedriften</button>
            </form>
        </Welcome>
    );
}
