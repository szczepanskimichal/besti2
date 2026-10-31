import type { ReactNode } from "react";
import styles from "./Welcome.module.css";

// Sparks flying out of the star: angle, distance and colour of each one
const COLORS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];
const SPARKS = Array.from({ length: 14 }, (_, k) => ({
    angle: `${k * 26 + 8}deg`,
    distance: `${86 + (k % 3) * 16}px`,
    color: COLORS[k % COLORS.length],
    shape: k % 2 ? "✦" : "✧",
}));

// "Velkommen": the crystal star pops out with sparks, then the title, the text and the button come up.
// Used once after the e-mail is confirmed (customers) and on the first visit to the panel (businesses)
// big = a short line under the title ("Nå er du en besti") in bigger letters
export default function Welcome({ title, text, big = false, children }: {
    title: ReactNode; text: string; big?: boolean; children: ReactNode;
}) {
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
            <h1 className={styles.title}>{title}</h1>
            <p className={big ? `${styles.info} ${styles.big}` : styles.info}>{text}</p>
            <div className={styles.action}>{children}</div>
        </main>
    );
}
