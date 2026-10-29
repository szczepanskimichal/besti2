import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "../../admin.module.css";

type NoteFormProps = {
    id: string;
    note: string;
    action: (formData: FormData) => Promise<void>;
};

// Who called when, what the business needs help with; never shown to the business or customers
export default function NoteForm({ id, note, action }: NoteFormProps) {
    return (
        <form action={action} className={styles.noteForm}>
            <input type="hidden" name="id" value={id}/>
            <label className={styles.srOnly} htmlFor="admin-notat">Notat</label>
            <textarea id="admin-notat" name="note" defaultValue={note} maxLength={2000} rows={5}
                      placeholder="F.eks. «29.10 ringt Kari, vil ha hjelp med bilder. Ring igjen fredag.»"
                      className={styles.textarea}/>
            <SubmitButton className={styles.primaryButton} pendingText="Lagrer …">Lagre notat</SubmitButton>
        </form>
    );
}
