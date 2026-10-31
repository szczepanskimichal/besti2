import type { CSSProperties } from "react";
import styles from "./page.module.css";

// The same "random" star dust on every visit (and on the server and in the browser)
function dust(count: number) {
    let seed = 7;
    const next = () => (seed = (seed * 9301 + 49297) % 233280) / 233280;
    return Array.from({ length: count }, () => {
        const size = 2.5 + next() * 3.5;
        return {
            left: `calc(50% + ${Math.round((next() - 0.5) * 230)}px)`,
            top: `calc(50% + ${Math.round((next() - 0.5) * 90)}px)`,
            width: `${size.toFixed(1)}px`,
            height: `${size.toFixed(1)}px`,
            animationDelay: `${(1.1 + next() * 0.6).toFixed(2)}s`,
            animationDuration: `${(1.6 + next() * 1.2).toFixed(2)}s`,
            "--dx": `${Math.round((next() - 0.5) * 24)}px`,
            "--dy": `${Math.round(-20 - next() * 40)}px`,
        } as CSSProperties;
    });
}

const DUST = dust(46);

// The number of visits draws itself with a mint line, then fills in, with star dust and a soft aurora
// behind it. Only CSS, once when the page opens (about 2 seconds); still for people who turned animations off
export default function DrawnNumber({ value }: { value: number }) {
    const text = String(value);
    const width = Math.max(200, text.length * 70);
    return (
        <div className={styles.number}>
            <span className={styles.aurora} aria-hidden="true"/>
            <svg viewBox={`0 0 ${width} 110`} className={styles.big} role="img" aria-label={text}>
                <text x={width / 2} y="92" textAnchor="middle">{text}</text>
            </svg>
            {DUST.map((style, i) => <span key={i} className={styles.dust} style={style} aria-hidden="true"/>)}
        </div>
    );
}
