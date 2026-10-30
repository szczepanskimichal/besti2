import Link from "next/link";
import { getCategoryLabel } from "@/lib/categories";
import { formatDate } from "@/lib/format";
import type { AdminBusinessRow } from "@/lib/types";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import styles from "./admin.module.css";

const statusLabels: Record<AdminBusinessRow["status"], string> = {
    Venter: "Venter på godkjenning",
    Synlig: "Synlig",
    Ubekreftet: "E-post ikke bekreftet",
    Stengt: "Stengt av BESTI",
    Skjult: "Skjult",
};

// One card per business, with what Ada needs to decide who to call
export default function BusinessRows({ rows }: { rows: AdminBusinessRow[] }) {
    return (
        <ul className={styles.list}>
            {rows.map((row) => (
                <li key={row.id} className={styles.card}>
                    <div className={styles.cardHeader}>
                        <Link href={`/admin/bedrifter/${row.id}`} className={styles.name}>{row.name}</Link>
                        <span className={`${styles.status} ${styles[row.status]}`}>{statusLabels[row.status]}</span>
                    </div>
                    <p className={styles.muted}>
                        {getCategoryLabel(row.category)} · {row.city} · registrert {formatDate(row.createdUtc)}
                    </p>
                    <p className={styles.facts}>
                        <span>Kom i gang {row.setupDone}/{row.setupTotal}</span>
                        <span>{row.bookingsLast30Days} bestillinger (30 d)</span>
                        <span>{row.upcoming} kommende</span>
                        {row.averageStars !== null && <span>{row.averageStars.toString().replace(".", ",")} <CrystalIcon name="stjerne"/></span>}
                        {row.verified && <span>Verifisert</span>}
                        {row.proInterest && <span className={styles.highlight}>Interessert i Pro</span>}
                        {row.hasNote && <span>📝 Notat</span>}
                    </p>
                </li>
            ))}
        </ul>
    );
}
