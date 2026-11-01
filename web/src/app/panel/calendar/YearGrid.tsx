import Link from "next/link";
import { formatDay, formatMonth, monthGrid, todayInOslo, weekdayOf } from "@/lib/format";
import type { BookingDayCount, OpeningHours } from "@/lib/types";
import { daysBetween } from "./calendarDates";
import calendar from "../calendar.module.css";

// The busier the day, the stronger the aurora colour (like a heat map); thresholds for a small salon
function level(count: number): string {
    if (count === 0) return "";
    if (count <= 2) return calendar.heat1;
    if (count <= 5) return calendar.heat2;
    if (count <= 9) return calendar.heat3;
    return calendar.heat4;
}

// Year: twelve small months with every day coloured by how many visits it has; a click opens the day,
// the month name opens the month. No prefetching: 365 links would load 365 pages
export default function YearGrid({ year, counts, dayHref, monthHref, openingHours = [] }: {
    year: string;
    openingHours?: OpeningHours[];
    counts: BookingDayCount[];
    dayHref: (date: string) => string;
    monthHref: (date: string) => string;
}) {
    const today = todayInOslo();
    const byDay = new Map(counts.map((c) => [c.date, c.count]));
    // Days the business is always closed (no opening hours that weekday): the same soft stripes as in the month
    const closed = (date: string) => openingHours.length > 0 && !openingHours.some((h) => h.dayOfWeek === weekdayOf(date));
    return (
        <>
            {/* What the colours mean, at the top like in the other views */}
            <div className={calendar.legend} aria-label="Forklaring">
                <ul>
                    <li className={calendar.legendTitle}>Timer per dag:</li>
                    <li><span className={`${calendar.yearSwatch} ${calendar.heat1}`}/>1–2</li>
                    <li><span className={`${calendar.yearSwatch} ${calendar.heat2}`}/>3–5</li>
                    <li><span className={`${calendar.yearSwatch} ${calendar.heat3}`}/>6–9</li>
                    <li><span className={`${calendar.yearSwatch} ${calendar.heat4}`}/>10+</li>
                </ul>
                <ul>
                    <li><span className={`${calendar.yearSwatch} ${calendar.yearClosed}`}/>Stengt</li>
                    <li><span className={`${calendar.yearSwatch} ${calendar.yearSwatchToday}`}/>I dag</li>
                </ul>
            </div>
            <div className={calendar.year}>
                {Array.from({ length: 12 }, (_, i) => `${year}-${String(i + 1).padStart(2, "0")}`).map((month) => {
                    const { gridStart, gridEnd } = monthGrid(month);
                    const total = counts.filter((c) => c.date.startsWith(month)).reduce((sum, c) => sum + c.count, 0);
                    return (
                        <section key={month} className={`${calendar.yearMonth} ${month === today.slice(0, 7) ? calendar.yearNow : ""}`}>
                            <Link href={monthHref(`${month}-01`)} prefetch={false} className={calendar.yearName}>
                                {formatMonth(month).split(" ")[0]}
                                <span className={calendar.yearTotal}>{total === 0 ? "–" : `${total} timer`}</span>
                            </Link>
                            <div className={calendar.yearDays}>
                                {daysBetween(gridStart, gridEnd).map((date) => {
                                    if (!date.startsWith(month)) {
                                        return <span key={date} aria-hidden="true"/>;
                                    }
                                    const count = byDay.get(date) ?? 0;
                                    return (
                                        <Link key={date} href={dayHref(date)} prefetch={false}
                                              className={`${calendar.yearDay} ${level(count)} ${count === 0 && closed(date) ? calendar.yearClosed : ""} ${date === today ? calendar.yearToday : ""}`}
                                              aria-label={`${formatDay(date)}: ${count} ${count === 1 ? "time" : "timer"}`}
                                              title={`${count} ${count === 1 ? "time" : "timer"}`}>
                                            {Number(date.slice(8))}
                                        </Link>
                                    );
                                })}
                            </div>
                        </section>
                    );
                })}
            </div>
        </>
    );
}
