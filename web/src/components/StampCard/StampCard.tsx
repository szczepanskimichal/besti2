import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import styles from "./StampCard.module.css";

type StampCardProps = {
    stamps: number;
    visits: number;
    small?: boolean;
    crystal?: boolean; // crystal hearts and a crystal gift in mint (Min Besti) instead of ✓ and 🎁
};

// "Stempelkort": one circle per visit, filled for each stamp; the last circle is the reward
export default function StampCard({ stamps, visits, small = false, crystal = false }: StampCardProps) {
    return (
        <ol className={`${styles.stamps} ${small ? styles.small : ""} ${crystal ? styles.crystal : ""}`}
            aria-label={`${stamps} av ${visits} stempler`}>
            {Array.from({ length: visits }, (_, i) => (
                <li key={i} aria-hidden="true"
                    className={`${styles.stamp} ${i < stamps ? styles.filled : ""} ${i === visits - 1 ? styles.reward : ""}`}>
                    {crystal
                        ? (i === visits - 1 ? <CrystalIcon name="gave"/> : i < stamps && <CrystalIcon name="hjerte"/>)
                        : (i === visits - 1 ? "🎁" : i < stamps ? "✓" : "")}
                </li>
            ))}
        </ol>
    );
}
