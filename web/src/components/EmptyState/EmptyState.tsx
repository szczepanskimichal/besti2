import Link from "next/link";
import type { ReactNode } from "react";
import styles from "./EmptyState.module.css";

type Props = {
    icon: ReactNode; // an emoji or a CrystalIcon
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
            <span className={styles.icon} aria-hidden="true">{icon}</span>
            <p className={styles.title}>{title}</p>
            <p className={styles.text}>{children}</p>
            {action && <Link href={action.href} className={styles.action}>{action.label}</Link>}
        </div>
    );
}
