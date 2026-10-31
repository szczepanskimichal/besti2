// Thin line icons (Ada's set) for Varsler and Mitt år, drawn in mint inside a glass circle
const PATHS = {
    ok: <><circle cx="12" cy="12" r="9"/><path d="M8 12.5l2.6 2.6L16 9.5"/></>,
    calendar: <><rect x="4" y="5" width="16" height="15" rx="3"/><path d="M4 10h16M9 3v4M15 3v4"/></>,
    bell: <><path d="M6 16V11a6 6 0 0 1 12 0v5l1.5 2h-15z"/><path d="M10 20a2 2 0 0 0 4 0"/></>,
    gear: <><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1"/></>,
    speaker: <><path d="M4 10v4h4l5 4V6L8 10z"/><path d="M16 9a4 4 0 0 1 0 6M18.5 6.5a7.5 7.5 0 0 1 0 11"/></>,
    mail: <><rect x="3.5" y="5.5" width="17" height="13" rx="3"/><path d="M4 7l8 6 8-6"/></>,
    clock: <><circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3 2"/></>,
    phone: <><rect x="7" y="2.5" width="10" height="19" rx="2.5"/><path d="M11 18.5h2"/></>,
    bin: <path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13M10 11v6M14 11v6"/>,
    hourglass: <><path d="M5 3h14M5 21h14"/><path d="M7 3v2.5L12 12l5-6.5V3M7 21v-2.5L12 12l5 6.5V21"/>
        <path d="M8.6 19.6 12 15.2l3.4 4.4z" fill="currentColor" stroke="none"/></>,
    scissors: <><circle cx="6" cy="7" r="2.6"/><circle cx="6" cy="17" r="2.6"/><path d="M8.2 8.4 20 17M8.2 15.6 20 7"/></>,
    people: <><circle cx="8.5" cy="8" r="3"/><circle cx="16.5" cy="9" r="2.5"/>
        <path d="M3 20c0-3.5 2.6-6 5.5-6s5.5 2.5 5.5 6M14 14.5c.8-.4 1.6-.6 2.5-.6 2.6 0 4.5 2.2 4.5 5.6"/></>,
    card: <><rect x="3" y="6" width="18" height="12" rx="3"/><path d="M3 10h18M7 14.5h4"/></>,
};

export type LineIconName = keyof typeof PATHS;

export default function LineIcon({ name }: { name: LineIconName }) {
    return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round"
             strokeLinejoin="round" aria-hidden="true">
            {PATHS[name]}
        </svg>
    );
}
