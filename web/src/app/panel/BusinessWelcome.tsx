import { welcomeSeenAction } from "./actions";
import styles from "./BusinessWelcome.module.css";

// Sparks flying out of the star: angle, distance and colour of each one
const COLORS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];
const SPARKS = Array.from({ length: 14 }, (_, k) => ({
    angle: `${k * 26 + 8}deg`,
    distance: `${86 + (k % 3) * 16}px`,
    color: COLORS[k % COLORS.length],
    shape: k % 2 ? "✦" : "✧",
}));

// "Velkommen til Besti": the crystal star pops out with sparks, shown once to a new business
export default function BusinessWelcome({ name, waiting }: { name: string; waiting: boolean }) {
    return (
        <main className={styles.main}>
            <div className={styles.gem} aria-hidden="true">
                <span className={styles.halo}/>
                <span className={styles.burst}/>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img src="/ikoner/stjerne.svg" alt="" className={styles.star}/>
                {SPARKS.map((spark) => (
                    <i key={spark.angle} className={styles.spark}
                       style={{ "--a": spark.angle, "--d": spark.distance, color: spark.color } as React.CSSProperties}>
                        {spark.shape}
                    </i>
                ))}
            </div>
            <h1 className={styles.title}>Velkommen til BESTI,<br/>{name}!</h1>
            <p className={styles.info}>
                {waiting
                    ? "Vi ser over bedriften din. Imens kan du legge inn tjenester og åpningstider."
                    : "Legg inn tjenester og åpningstider, så kan kundene bestille."}
            </p>
            <form action={welcomeSeenAction}>
                <button type="submit" className={styles.go}>Sett opp bedriften</button>
            </form>
        </main>
    );
}
