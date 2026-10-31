import { notificationSettingsAction } from "./actions";
import LineIcon from "./LineIcon";
import styles from "./page.module.css";

type Props = { reminderEmail: boolean; soonNotice: boolean };

// Customers choose which reminders before a visit they get (both are on for a new account)
export default function ReminderSettings({ reminderEmail, soonNotice }: Props) {
    const choices = [
        { key: "reminderEmail", label: "E-post dagen før timen", icon: "mail", on: reminderEmail },
        { key: "soonNotice", label: "Varsel 2 timer før", icon: "clock", on: soonNotice },
    ] as const;
    return (
        <div className={styles.reminders}>
            {choices.map((choice) => (
                <form key={choice.key} action={notificationSettingsAction}>
                    <input type="hidden" name="reminderEmail"
                           value={String(choice.key === "reminderEmail" ? !reminderEmail : reminderEmail)}/>
                    <input type="hidden" name="soonNotice"
                           value={String(choice.key === "soonNotice" ? !soonNotice : soonNotice)}/>
                    <button type="submit" className={styles.row} role="switch" aria-checked={choice.on}>
                        <span className={styles.icon}><LineIcon name={choice.icon}/></span>
                        <span className={styles.rowLabel}>{choice.label}</span>
                        <span className={styles.switch} aria-hidden="true"/>
                    </button>
                </form>
            ))}
        </div>
    );
}
