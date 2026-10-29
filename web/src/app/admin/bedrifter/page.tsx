import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getAdminBusinesses } from "@/lib/api";
import { getToken } from "@/lib/auth";
import BusinessRows from "../BusinessRows";
import SearchForm from "./SearchForm";
import styles from "../admin.module.css";

export const metadata: Metadata = { title: "Bedrifter" };

const filters = [
    { value: "", label: "Alle" },
    { value: "Venter", label: "Venter" },
    { value: "Synlig", label: "Synlige" },
    { value: "Ubekreftet", label: "Ikke bekreftet" },
    { value: "Stengt", label: "Stengt" },
];

type BusinessesPageProps = {
    searchParams: Promise<{ status?: string; q?: string }>;
};

export default async function AdminBusinessesPage({ searchParams }: BusinessesPageProps) {
    const query = await searchParams;
    const status = filters.find((f) => f.value === query.status)?.value ?? "";
    const q = (query.q ?? "").slice(0, 100);
    const token = await getToken();
    const rows = token ? await getAdminBusinesses(token, status, q) : null;
    if (!rows) {
        notFound();
    }

    return (
        <>
            <h1 className={styles.title}>Bedrifter</h1>
            <nav className={styles.filters} aria-label="Status">
                {filters.map((f) => (
                    <Link key={f.value} href={`/admin/bedrifter?${new URLSearchParams({ ...(f.value ? { status: f.value } : {}), ...(q ? { q } : {}) })}`}
                          className={f.value === status ? `${styles.filter} ${styles.filterActive}` : styles.filter}
                          aria-current={f.value === status ? "page" : undefined}>
                        {f.label}
                    </Link>
                ))}
            </nav>
            <SearchForm status={status} q={q}/>
            <p className={styles.muted}>{rows.length} {rows.length === 1 ? "bedrift" : "bedrifter"}</p>
            <BusinessRows rows={rows}/>
        </>
    );
}
