import styles from "./ProBadge.module.css";

type Props = {
    // Only "Pro" where space is short (menu, period buttons); the full text everywhere else
    short?: boolean;
};

// Marks the features that become part of BESTI Pro when payments start. Free for everyone in the beta
export default function ProBadge({ short }: Props) {
    return (
        <span className={styles.badge} title="Gratis i beta. Blir en del av BESTI Pro når betaling starter.">
            {short ? "Pro" : "Pro · gratis i beta"}
        </span>
    );
}
