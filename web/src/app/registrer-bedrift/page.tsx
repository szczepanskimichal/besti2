import type { Metadata } from "next";
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
            <p className={styles.muted}>Få kundene dine til å bestille time på nett.</p>
            <RegisterBusinessForm />
        </main>
    );
}