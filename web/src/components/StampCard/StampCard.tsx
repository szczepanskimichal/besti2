import styles from "./StampCard.module.css";

type StampCardProps = {
    stamps: number;
    visits: number;
    small?: boolean;
};

// "Stempelkort": one circle per visit, filled for each stamp; the last circle is the reward
export default function StampCard({ stamps, visits, small = false }: StampCardProps) {
    return (
        <ol className={small ? `${styles.stamps} ${styles.small}` : styles.stamps}
            aria-label={`${stamps} av ${visits} stempler`}>
            {Array.from({ length: visits }, (_, i) => (
                <li key={i} aria-hidden="true"
                    className={`${styles.stamp} ${i < stamps ? styles.filled : ""} ${i === visits - 1 ? styles.reward : ""}`}>
                    {i === visits - 1 ? "🎁" : i < stamps ? "✓" : ""}
                </li>
            ))}
        </ol>
    );
}
