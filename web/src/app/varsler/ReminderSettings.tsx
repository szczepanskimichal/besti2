import SubmitButton from "@/components/SubmitButton/SubmitButton";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import { notificationSettingsAction } from "./actions";
import styles from "./page.module.css";

type Props = { reminderEmail: boolean; soonNotice: boolean };

// Customers choose which reminders before a visit they get (both are on for a new account)
export default function ReminderSettings({ reminderEmail, soonNotice }: Props) {
    const choices = [
        { key: "reminderEmail", label: "E-post dagen før timen", on: reminderEmail },
        { key: "soonNotice", label: "Varsel 2 timer før (Snart tid!)", on: soonNotice },
    ];
    return (
        <div className={styles.reminders}>
            {choices.map((choice) => (
                <form key={choice.key} action={notificationSettingsAction}>
                    <input type="hidden" name="reminderEmail"
                           value={String(choice.key === "reminderEmail" ? !reminderEmail : reminderEmail)}/>
                    <input type="hidden" name="soonNotice"
                           value={String(choice.key === "soonNotice" ? !soonNotice : soonNotice)}/>
                    <SubmitButton className={`${styles.soundToggle} ${choice.on ? "" : styles.off}`} pendingText="Lagrer…">
                        <CrystalIcon name="bjelle" off={!choice.on}/>
                        {choice.label}: <strong>{choice.on ? "På" : "Av"}</strong>
                    </SubmitButton>
                </form>
            ))}
        </div>
    );
}
