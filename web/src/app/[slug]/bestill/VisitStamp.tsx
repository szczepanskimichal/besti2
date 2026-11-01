import styles from "./MonthCalendar.module.css";

// The customer's own visit, the same small stamp as in the business calendar (BESTI colour rule):
// mint check = booked, lilac clock = still waiting for an answer
export default function VisitStamp({ waiting = false, className = "" }: { waiting?: boolean; className?: string }) {
    return (
        <span className={`${styles.visitStamp} ${waiting ? styles.visitWait : styles.visitOk} ${className}`} aria-hidden="true">
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round">
                {waiting
                    ? <><circle cx="8" cy="8" r="6.6"/><path d="M8 3.8V8l2.9 1.8"/></>
                    : <><circle cx="8" cy="8" r="6.6"/><path d="M4.8 8.3l2.2 2.2 4.3-4.8"/></>}
            </svg>
        </span>
    );
}
