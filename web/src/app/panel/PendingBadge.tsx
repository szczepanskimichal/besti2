import calendar from "./calendar.module.css";

// Visits that are not confirmed yet get a clock and a short label, not only a colour
const pendingLabels: Record<string, string> = {
    Venter: "Venter",
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
