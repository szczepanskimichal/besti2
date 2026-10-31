import { getCategoryColor } from "@/lib/categories";
import styles from "./BusinessLogo.module.css";

// "Klipp & Stil Frisør" -> "KS", "Fjord Bygg AS" -> "FB"
export function getInitials(name: string): string {
    const words = name
        .split(/\s+/)
        .filter((w) => /^[\p{L}\p{N}]/u.test(w) && w.toUpperCase() !== "AS");
    return words.slice(0, 2).map((w) => w[0]).join("").toUpperCase();
}

type Props = {
    name: string;
    category: string;
    logoUrl?: string | null;
    size?: "small" | "large";
};

export default function BusinessLogo({ name, category, logoUrl, size = "small" }: Props) {
// Uploaded logo when the business has one, otherwise initials
    if (logoUrl) {
        return (
// eslint-disable-next-line @next/next/no-img-element -- small logo from our own API, next/image not needed
            <img src={logoUrl} alt="" className={`${styles.logo} ${styles.image} ${styles[size]}`} />
        );
    }
    return (
        <span
            className={`${styles.logo} ${styles[size]}`}
            style={{ backgroundColor: getCategoryColor(category) }}
            aria-hidden="true"
        >
            {getInitials(name)}
        </span>
    );
}