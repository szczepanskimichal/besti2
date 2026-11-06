import styles from "./HomeSky.module.css";

// Extra life in the sky behind the front page only (Ada 2026-11-06): a soft green glow and 30 small stars.
// Phone: the glow breathes slowly and the stars twinkle (only opacity and transform, cheap for the phone).
// Computer and "reduce motion": the same glow and stars, standing still.
// The stars have fixed places (made once), so the page looks the same every time and needs no JavaScript.
const STARS = [
    { left: 49.0, top: 30.8, delay: -0.46, duration: 3.61, big: true },
    { left: 44.3, top: 27.9, delay: -0.31, duration: 5.2, big: false },
    { left: 58.9, top: 22.6, delay: -1.14, duration: 4.95, big: false },
    { left: 65.7, top: 4.2, delay: -1.99, duration: 3.59, big: false },
    { left: 67.2, top: 8.7, delay: -2.24, duration: 4.22, big: false },
    { left: 17.6, top: 2.8, delay: -3.76, duration: 5.42, big: true },
    { left: 12.1, top: 24.4, delay: -3.82, duration: 3.47, big: false },
    { left: 41.9, top: 30.3, delay: -2.69, duration: 3.7, big: false },
    { left: 17.0, top: 54.2, delay: -1.78, duration: 3.58, big: false },
    { left: 54.7, top: 18.2, delay: -0.61, duration: 4.93, big: false },
    { left: 35.2, top: 19.3, delay: -0.32, duration: 5.32, big: true },
    { left: 23.9, top: 48.6, delay: -1.88, duration: 3.59, big: false },
    { left: 60.3, top: 22.1, delay: -3.04, duration: 4.63, big: false },
    { left: 13.6, top: 12.3, delay: -1.8, duration: 3.23, big: false },
    { left: 4.8, top: 22.7, delay: -2.72, duration: 4.62, big: false },
    { left: 95.5, top: 17.9, delay: -1.75, duration: 4.24, big: true },
    { left: 43.9, top: 46.1, delay: -1.8, duration: 4.1, big: false },
    { left: 26.4, top: 13.0, delay: -2.97, duration: 3.33, big: false },
    { left: 27.0, top: 42.2, delay: -3.43, duration: 5.42, big: false },
    { left: 92.7, top: 47.3, delay: -2.99, duration: 4.6, big: false },
    { left: 55.5, top: 1.3, delay: -1.29, duration: 3.89, big: true },
    { left: 21.5, top: 21.6, delay: -2.74, duration: 4.84, big: false },
    { left: 26.7, top: 52.6, delay: -0.03, duration: 3.9, big: false },
    { left: 58.6, top: 24.3, delay: -0.36, duration: 5.48, big: false },
    { left: 45.6, top: 48.0, delay: -2.35, duration: 4.17, big: false },
    { left: 12.5, top: 43.6, delay: -3.59, duration: 3.66, big: true },
    { left: 2.4, top: 8.5, delay: -2.83, duration: 4.74, big: false },
    { left: 66.0, top: 47.8, delay: -0.92, duration: 5.08, big: false },
    { left: 16.9, top: 2.1, delay: -0.33, duration: 3.05, big: false },
    { left: 53.7, top: 45.7, delay: -3.37, duration: 3.36, big: false },
];

export default function HomeSky() {
    return (
        <div className={styles.sky} data-home-sky="" aria-hidden="true">
            <div className={styles.glow}/>
            {STARS.map((star, k) => (
                <i key={k} className={star.big ? `${styles.star} ${styles.big}` : styles.star}
                   style={{ left: `${star.left}%`, top: `${star.top}%`, animationDelay: `${star.delay}s`, animationDuration: `${star.duration}s` }}/>
            ))}
        </div>
    );
}
