import type { Metadata } from "next";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import RegisterBusinessForm from "./RegisterBusinessForm";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Registrer bedriften din",
    description: "Få kundene dine til å bestille time på nett.",
};

export default function RegisterBusinessPage() {
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Registrer bedriften din</h1>
            <p className={styles.lead}>Få kundene dine til å bestille time på nett.</p>
            <p className={styles.glad}><CrystalIcon name="stjerne"/>Vi gleder oss til å ha deg med i BESTI!</p>
            <RegisterBusinessForm />
        </main>
    );
}