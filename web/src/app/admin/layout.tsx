import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { privatePage } from "@/lib/site";
import styles from "./admin.module.css";

export const metadata: Metadata = { title: { default: "Admin", template: "%s | Admin | BESTI" }, robots: privatePage };

// Only for the BESTI team (Admin:Emails on the server). Everyone else gets "Fant ikke siden", so the page
// does not even show that it exists
export default async function AdminLayout({ children }: { children: React.ReactNode }) {
    const token = await getToken();
    const me = token ? await getMe(token) : null;
    if (!me?.isAdmin) {
        notFound();
    }
    return (
        <main className={styles.main}>
            <div className={styles.topBar}>
                <p className={styles.kicker}>BESTI admin</p>
                <nav className={styles.nav} aria-label="Admin">
                    <Link href="/admin" className={styles.navLink}>Oversikt</Link>
                    <Link href="/admin/bedrifter" className={styles.navLink}>Bedrifter</Link>
                    <Link href="/admin/vurderinger" className={styles.navLink}>Vurderinger</Link>
                </nav>
            </div>
            {children}
        </main>
    );
}
