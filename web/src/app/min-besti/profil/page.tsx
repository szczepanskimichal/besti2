import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getMyProfile } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatMonth, osloDay } from "@/lib/format";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import MinBestiTabs from "@/components/MinBestiTabs/MinBestiTabs";
import ProfileForms from "./ProfileForms";
import panelStyles from "../../panel/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Min profil",
};

// "Min profil": name and phone, password, and "Slett konto"
export default async function ProfilePage() {
    const token = await getToken();
    if (!token) {
        redirect("/logg-inn?neste=/min-besti/profil");
    }
    // null = not a customer (a business owner) or logged out
    const profile = await getMyProfile(token);
    if (!profile) {
        redirect("/panel");
    }

    const firstName = profile.fullName.split(" ")[0];
    // "Ida Nordmann" -> "IN"
    const initials = profile.fullName.split(" ").filter(Boolean).slice(0, 2).map((part) => part[0].toUpperCase()).join("");

    return (
        <main className={panelStyles.main}>
            <h1 className={styles.title}>Min BESTI</h1>
            <MinBestiTabs/>
            <section className={styles.me}>
                <span className={styles.initials} aria-hidden="true">{initials}</span>
                <div>
                    <p className={styles.hello}>Hei, {firstName}! <CrystalIcon name="stjerne"/></p>
                    {profile.createdUtc && (
                        <p className={styles.since}>Din BESTI siden {formatMonth(osloDay(profile.createdUtc).slice(0, 7))}</p>
                    )}
                    <p className={styles.email}>{profile.email}</p>
                </div>
            </section>
            <div className={styles.sections}>
                <ProfileForms profile={profile}/>
            </div>
        </main>
    );
}
