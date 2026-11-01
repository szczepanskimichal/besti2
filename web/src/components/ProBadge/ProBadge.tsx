import type { CSSProperties } from "react";
import styles from "./ProBadge.module.css";

type Props = {
    // Only "Pro" where space is short (menu, period buttons); the full text everywhere else
    short?: boolean;
    // A bigger stamp next to a title or a price (px); 34 when left out
    size?: number;
};

// Marks the features that become part of BESTI Pro when payments start. Free for everyone in the beta
export default function ProBadge({ short, size }: Props) {
    return (
        <span className={styles.wrap} title="Gratis i beta. Blir en del av BESTI Pro når betaling starter.">
            {/* Round stamp: crystal crown on top, PRO under it, aurora neon ring (variant M) */}
            <span className={styles.badge} style={size ? { "--s": `${size}px` } as CSSProperties : undefined}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src="/ikoner/krone.svg" alt="" width={16} height={16} className={styles.crown}/>
                <span className={styles.text}>Pro</span>
            </span>
            {!short && <span className={styles.note}>Gratis i beta</span>}
        </span>
    );
}
