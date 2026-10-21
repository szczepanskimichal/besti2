import styles from "./Stars.module.css";

type Props = {
    value: number; // 1–5, may be a decimal like 4.5
};

// Shows a rating as stars; screen readers get "4,5 av 5 stjerner"
export default function Stars({ value }: Props) {
    const rounded = Math.round(value);
    return (
        <span className={styles.stars} role="img" aria-label={`${value.toString().replace(".", ",")} av 5 stjerner`}>
            <span aria-hidden="true">
                <span className={styles.on}>{"★".repeat(rounded)}</span>
                <span className={styles.off}>{"★".repeat(5 - rounded)}</span>
            </span>
        </span>
    );
}