import type { PanelBooking } from "@/lib/types";
import calendar from "../calendar.module.css";

// The top of the day: how many visits, how many still wait for an answer and how many hours are booked.
// Three crystal numbers over the whole width, so the owner sees the day at a glance before the list
export default function DaySummary({ bookings }: { bookings: PanelBooking[] }) {
    const visits = bookings.filter((b) => b.status !== "Avlyst");
    const waiting = visits.filter((b) => b.status === "Venter" || b.status === "VenterKunde").length;
    const minutes = visits.reduce((sum, b) => sum + (Date.parse(b.endUtc) - Date.parse(b.startUtc)) / 60000, 0);
    const hours = Math.round(minutes / 6) / 10;

    return (
        <section className={calendar.summary} aria-label="Dagen">
            <div className={calendar.summaryStats}>
                <p className={calendar.summaryVisits}><strong>{visits.length}</strong><span>{visits.length === 1 ? "time" : "timer"}</span></p>
                <p className={waiting ? calendar.summaryWaiting : undefined}>
                    <strong>{waiting}</strong><span>venter</span>
                </p>
                <p className={calendar.summaryBooked}><strong>{hours.toLocaleString("nb-NO")}</strong><span>timer booket</span></p>
            </div>
        </section>
    );
}
