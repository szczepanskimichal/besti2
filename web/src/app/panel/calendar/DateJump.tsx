"use client";

import type { ReactNode } from "react";
import calendar from "../calendar.module.css";

// The date itself is the button: tap it, pick a day, the calendar jumps there (no "Vis" button)
export default function DateJump({ day, view, employee, children }: { day: string; view?: string; employee?: string; children: ReactNode }) {
    return (
        <form method="get" className={calendar.dateJump}>
            {view && <input type="hidden" name="visning" value={view}/>}
            {employee && <input type="hidden" name="ansatt" value={employee}/>}
            <label>
                {children}
                <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8"
                     strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <path d="M4 6h16v14H4zM4 10h16M8 3v4M16 3v4M8 14h.01M12 14h.01M16 14h.01M8 17h.01M12 17h.01"/>
                </svg>
                <input type="date" name="dag" defaultValue={day} aria-label="Velg dato"
                       onClick={(e) => { try { e.currentTarget.showPicker(); } catch { /* older browsers open it themselves */ } }}
                       onChange={(e) => { if (e.currentTarget.value) e.currentTarget.form?.requestSubmit(); }}/>
            </label>
        </form>
    );
}
