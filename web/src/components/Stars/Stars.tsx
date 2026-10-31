import styles from "./Stars.module.css";

type Props = {
    value: number; // 1–5, may be a decimal like 4.5
};

// Shows a rating as crystal stars in the aurora colours; screen readers get "4,5 av 5 stjerner"
export default function Stars({ value }: Props) {
    // A star only lights up when it is (almost) earned: 4,6 shows 4 stars, from 4,75 it is 5
    const rounded = Math.floor(value + 0.25);
    return (
        <span className={styles.stars} role="img" aria-label={`${value.toString().replace(".", ",")} av 5 stjerner`}>
            {[1, 2, 3, 4, 5].map((n) => (
                // eslint-disable-next-line @next/next/no-img-element -- tiny SVG icon, next/image not needed
                <img key={n} src={n <= rounded ? "/ikoner/stjerne-liten.svg" : "/ikoner/stjerne-tom.svg"} alt=""
                     className={n <= rounded ? undefined : styles.off}/>
            ))}
        </span>
    );
}
