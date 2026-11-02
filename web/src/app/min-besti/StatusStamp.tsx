import { statusLabels } from "@/lib/bookingStatus";
import mine from "./bookings.module.css";

// Every status as a tilted glowing stamp in the corner: mint = confirmed, lavender = waiting
// (for the business, or for the customer's answer to a new time)
export default function StatusStamp({ status, className = "" }: { status: string; className?: string }) {
    const waiting = status !== "Bekreftet";
    return (
        <span className={`${mine.stamp} ${waiting ? mine.stampWaiting : ""} ${className}`} role="img"
              aria-label={`Status: ${statusLabels[status] ?? status}`}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.6} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                {waiting ? <><circle cx="12" cy="12" r="8.5"/><path d="M12 7.5v4.8l3.2 2"/></> : <path d="M5 12.5l4.5 4.5L19 7.5"/>}
            </svg>
            <span aria-hidden="true">
                {!waiting ? "BEKREFTET" : status === "VenterKunde" ? <>VENTER PÅ<small>SVARET DITT</small></> : <>VENTER PÅ<small>BEKREFTELSE</small></>}
            </span>
        </span>
    );
}
