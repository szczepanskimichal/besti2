import calendar from "./calendar.module.css";

// Visits that are not confirmed yet get a clock and a short label, not only a colour
const pendingLabels: Record<string, string> = {
    Venter: "Venter på din bekreftelse",
    VenterKunde: "Venter på kunden",
};

export default function PendingBadge({ status, iconOnly = false }: { status: string; iconOnly?: boolean }) {
    if (status !== "Venter" && status !== "VenterKunde") {
        return null;
    }
    const label = pendingLabels[status];
    return (
        <span className={iconOnly ? calendar.pendingIcon : calendar.pending}>
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor"
                 strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
                <circle cx="8" cy="8" r="6.5"/>
                <path d="M8 4.5V8l2.5 1.5"/>
            </svg>
            {iconOnly ? <span className={calendar.srOnly}>{label}</span> : label}
        </span>
    );
}

// Round mint stamp on visits that are confirmed
export function OkStamp() {
    return (
        <span className={calendar.stampOk}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor"
                 strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M3.5 8.5l3 3 6-7"/>
            </svg>
            Bekreftet
        </span>
    );
}

// Small round stamp next to the time: lilac clock = waiting, mint check = confirmed.
// The symbol fills the whole stamp, so it stays readable even when small and never covers the name
export function MiniStamp({ status }: { status: string }) {
    const waiting = status === "Venter" || status === "VenterKunde";
    if (!waiting && status !== "Bekreftet") {
        return null;
    }
    const label = waiting ? pendingLabels[status] : "Bekreftet";
    return (
        <span className={`${calendar.miniStamp} ${waiting ? calendar.miniWait : calendar.miniOk}`} title={label}>
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.9"
                 strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                {waiting
                    ? <><circle cx="8" cy="8" r="6.6"/><path d="M8 3.8V8l2.9 1.8"/></>
                    : <><circle cx="8" cy="8" r="6.6"/><path d="M4.8 8.3l2.2 2.2 4.3-4.8"/></>}
            </svg>
            <span className={calendar.srOnly}>{label}</span>
        </span>
    );
}
