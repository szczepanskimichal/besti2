import type { CSSProperties } from "react";
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
    // Even rows (Ada 2026-11-04): up to 6 in one row, more in two equal rows, e.g. 10 = 1–5 and 6–10.
    // The small card on the salon page stays one row
    const columns = visits <= 6 ? visits : Math.ceil(visits / 2);
    return (
        <ol className={`${styles.stamps} ${small ? styles.small : styles.rows} ${crystal ? styles.crystal : ""}`}
            style={small ? undefined : { "--columns": columns } as CSSProperties}
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
