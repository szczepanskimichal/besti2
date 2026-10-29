import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminLog } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDateTime } from "@/lib/format";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "Logg" };

// Who in the BESTI team did what and when (approvals, hidden reviews, closed businesses, notes). Kept 3 years
export default async function AdminLogPage() {
    const token = await getToken();
    const log = token ? await getAdminLog(token) : null;
    if (!log) {
        notFound();
    }

    return (
        <>
            <h1 className={styles.title}>Logg</h1>
            {log.length === 0 && <p className={styles.muted}>Ingenting er gjort ennå.</p>}
            <ul className={`${styles.card} ${styles.log}`}>
                {log.map((entry) => (
                    <li key={`${entry.createdUtc}-${entry.text}`}>
                        <span>
                            {entry.businessId ? <Link href={`/admin/bedrifter/${entry.businessId}`}>{entry.text}</Link> : entry.text}
                        </span>
                        <span className={styles.muted}>{formatDateTime(entry.createdUtc)} · {entry.adminEmail}</span>
                    </li>
                ))}
            </ul>
        </>
    );
}
