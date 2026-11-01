// A thin padlock (Ada 2026-11-01): marks what is locked until BESTI Pro, like SMS. Colour from the text around it
export default function LockIcon({ className, keyhole }: { className?: string; keyhole?: boolean }) {
    return (
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round"
             strokeLinejoin="round" className={className} aria-hidden="true">
            <rect x="4.5" y="10.5" width="15" height="10" rx="2.5"/>
            <path d="M8 10.5V7.5a4 4 0 0 1 8 0v3"/>
            {keyhole && <circle cx="12" cy="15.5" r="1.3" fill="currentColor"/>}
        </svg>
    );
}
