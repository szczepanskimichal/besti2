import RegisterBusinessForm from "./RegisterBusinessForm";
import styles from "./page.module.css";

export default function RegisterBusinessPage() {
    return (
        <main className={styles.main}>
            <h1 className={styles.title}>Registrer bedriften din</h1>
            <p className={styles.muted}>Få kundene dine til å bestille time på nett.</p>
            <RegisterBusinessForm />
        </main>
    );
}