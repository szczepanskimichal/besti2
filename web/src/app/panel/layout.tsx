import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { privatePage } from "@/lib/site";
import styles from "./page.module.css";

export const metadata: Metadata = { title: "Bedriftspanel", robots: privatePage };

// Every panel page is for business accounts only: a customer goes to Min Besti, a visitor to login
export default async function PanelLayout({ children }: { children: React.ReactNode }) {
    const token = await getToken();
    const me = token ? await getMe(token) : null;
    if (!me) {
        redirect("/logg-inn");
    }
    if (!me.isBusiness) {
        redirect("/min-besti");
    }
    return (
        <>
            <div className={styles.calmSky} aria-hidden="true"/>
            {children}
        </>
    );
}