import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { getMyProfile } from "@/lib/api";
import { getToken } from "@/lib/auth";
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

    return (
        <main className={panelStyles.main}>
            <h1 className={panelStyles.title}>Min Besti</h1>
            <MinBestiTabs/>
            <div className={styles.sections}>
                <ProfileForms profile={profile}/>
            </div>
        </main>
    );
}
