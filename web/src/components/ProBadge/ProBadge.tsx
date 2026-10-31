import styles from "./ProBadge.module.css";

type Props = {
    // Only "Pro" where space is short (menu, period buttons); the full text everywhere else
    short?: boolean;
};

// Marks the features that become part of BESTI Pro when payments start. Free for everyone in the beta
export default function ProBadge({ short }: Props) {
    return (
        <span className={styles.wrap} title="Gratis i beta. Blir en del av BESTI Pro når betaling starter.">
            {/* Round stamp: crystal crown on top, PRO under it, aurora neon ring (variant M) */}
            <span className={styles.badge}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src="/ikoner/krone.svg" alt="" width={16} height={16} className={styles.crown}/>
                <span className={styles.text}>Pro</span>
            </span>
            {!short && <span className={styles.note}>Gratis i beta</span>}
        </span>
    );
}
