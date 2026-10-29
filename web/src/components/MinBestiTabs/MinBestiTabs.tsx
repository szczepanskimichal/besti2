"use client";
import NextLink from "next/link";
import { usePathname } from "next/navigation";
import styles from "./MinBestiTabs.module.css";

const tabs = [
    { href: "/min-besti", label: "Bestillinger" },
    { href: "/min-besti/favoritter", label: "Favoritter" },
    { href: "/min-besti/mitt-ar", label: "Mitt år" },
    { href: "/min-besti/vurderinger", label: "Vurderinger" },
    { href: "/min-besti/profil", label: "Profil" },
];
// Tabs at the top of Min Besti; the current page is highlighted
export default function MinBestiTabs() {
    const pathname = usePathname();
    return (
        <nav aria-label="Min Besti" className={styles.tabs}>
            {tabs.map((tab) => {
                const active = pathname === tab.href;
                return (
                    <NextLink
                        key={tab.href}
                        href={tab.href}
                        className={active ? `${styles.tab} ${styles.active}` : styles.tab}
                        aria-current={active ? "page" : undefined}
                    >
                        {tab.label}
                    </NextLink>
                );
            })}
        </nav>
    );
}