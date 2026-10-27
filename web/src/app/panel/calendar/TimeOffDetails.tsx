import Link from "next/link";
import type { TimeOff } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { deleteTimeOffAction } from "../actions";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";
import { describeBlock } from "./calendarLayout";

// The details window for blocked time: when, who, and "Fjern blokkering"
export default function TimeOffDetails({ block, closeHref }: { block: TimeOff; closeHref: string }) {
    return (
        <div className={calendar.overlay}>
            <section className={calendar.dialog} role="dialog" aria-modal="true" aria-labelledby="time-off-details-title">
                <div className={calendar.dialogHead}>
                    <h2 id="time-off-details-title" className={calendar.dialogTitle}>{block.reason ?? "Blokkert tid"}</h2>
                    <Link href={closeHref} scroll={false} className={calendar.close} aria-label="Lukk">✕</Link>
                </div>
                <p className={calendar.dialogWhen}>{describeBlock(block)}</p>
                <p className={styles.service}>{block.employeeName ?? "Hele bedriften (stengt)"}</p>
                <form action={deleteTimeOffAction} className={styles.actions}>
                    <input type="hidden" name="timeOffId" value={block.id}/>
                    <input type="hidden" name="tilbake" value={closeHref}/>
                    <SubmitButton className={styles.dangerButton} pendingText="Fjerner…">Fjern blokkering</SubmitButton>
                </form>
            </section>
        </div>
    );
}
