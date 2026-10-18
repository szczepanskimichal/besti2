import styles from "./BusinessLogo.module.css";

// Dark enough for white text (WCAG contrast)
const categoryColors: Record<string, string> = {
    Frisor: "#be185d",
    Barber: "#6d28d9",
    Skjonnhet: "#a21caf",
    Bygg: "#c2410c",
    Rorlegger: "#0369a1",
    Elektriker: "#a16207",
    Helse: "#047857",
    Annet: "#475569",
};

// "Klipp & Stil Frisør" -> "KS", "Fjord Bygg AS" -> "FB"
function getInitials(name: string): string {
    const words = name
        .split(/\s+/)
        .filter((w) => /^[\p{L}\p{N}]/u.test(w) && w.toUpperCase() !== "AS");
    return words.slice(0, 2).map((w) => w[0]).join("").toUpperCase();
}

type Props = {
    name: string;
    category: string;
    size?: "small" | "large";
};

export default function BusinessLogo({ name, category, size = "small" }: Props) {
    return (
        <span
            className={`${styles.logo} ${styles[size]}`}
            style={{ backgroundColor: categoryColors[category] ?? categoryColors.Annet }}
            aria-hidden="true"
        >
            {getInitials(name)}
        </span>
    );
}