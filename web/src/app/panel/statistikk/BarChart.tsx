import styles from "./page.module.css";

type Bar = { label: string; value: number; detail: string };

// One series of bars in plain HTML: the height shows the number, the label under each bar says what it is,
// and every bar has its value as text for screen readers and on hover
// glow = the darker Min Besti look: a dark glass card and deep teal bars
export default function BarChart({ title, bars, glow = false }: { title: string; bars: Bar[]; glow?: boolean }) {
    const max = Math.max(1, ...bars.map((b) => b.value));
    const top = bars.reduce((best, b, i) => (b.value > bars[best].value ? i : best), 0);
    return (
        <figure className={glow ? `${styles.chart} ${styles.glow}` : styles.chart}>
            <figcaption className={styles.chartTitle}>{title}</figcaption>
            <ol className={bars.length > 8 ? `${styles.bars} ${styles.dense}` : styles.bars} style={{ gridTemplateColumns: `repeat(${bars.length}, minmax(0, 1fr))` }}>
                {bars.map((bar, i) => (
                    <li key={bar.label} className={styles.barItem} title={bar.detail} aria-label={bar.detail}>
                        <span className={styles.barValue} aria-hidden="true">
                            {i === top && bar.value > 0 ? bar.value : ""}
                        </span>
                        <span className={styles.barTrack} aria-hidden="true">
                            <span className={styles.bar} style={{ height: `${(bar.value / max) * 100}%` }}/>
                        </span>
                        <span className={styles.barLabel} aria-hidden="true">{bar.label}</span>
                    </li>
                ))}
            </ol>
        </figure>
    );
}
