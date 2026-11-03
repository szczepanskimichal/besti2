import Link from "next/link";
import type { ReactNode } from "react";
import styles from "./EmptyState.module.css";

// Ada 2026-11-03: crystal pictures in the neon ring instead of emoji in the panel's empty lists
const RING_ICONS = {
    notes: "/ikoner/notatblokk.svg", // Liste
    scissors: "/ikoner/saks.svg", // Tjenester
    people: "/ikoner/kunder.svg", // Kunder
};
type RingIcon = keyof typeof RING_ICONS;

type Props = {
    icon: ReactNode | "sparkles" | "star" | RingIcon; // an emoji, a CrystalIcon, three crystal sparkles, one big crystal star or a crystal picture in a neon ring
    title: string;
    children: ReactNode;
    // The one next step, e.g. "Finn en bedrift"
    action?: { href: string; label: string };
};

// A list with nothing in it yet: a friendly card that says why it is empty and what to do next,
// instead of a lone grey sentence
export default function EmptyState({ icon, title, children, action }: Props) {
    return (
        <div className={styles.box}>
            {icon === "sparkles" ? (
                <span className={styles.ring} aria-hidden="true">
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${styles.sparkle} ${styles.big}`}/>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${styles.sparkle} ${styles.small}`}/>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${styles.sparkle} ${styles.tiny}`}/>
                </span>
            ) : icon === "star" ? (
                // Ada 2026-11-03: the crystal star lit in the same neon ring as the sparkles
                <span className={styles.ring} aria-hidden="true">
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/stjerne-liten.svg" alt="" className={`${styles.sparkle} ${styles.ringStar}`}/>
                </span>
            ) : typeof icon === "string" && icon in RING_ICONS ? (
                <span className={styles.ring} aria-hidden="true">
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src={RING_ICONS[icon as RingIcon]} alt="" className={`${styles.sparkle} ${styles.ringPicture}`}/>
                </span>
            ) : (
                <span className={styles.icon} aria-hidden="true">{icon}</span>
            )}
            <p className={styles.title}>{title}</p>
            <p className={styles.text}>{children}</p>
            {action && <Link href={action.href} className={styles.action}>{action.label}</Link>}
        </div>
    );
}
