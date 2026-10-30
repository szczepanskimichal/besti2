import Link from "next/link";
import { formatDay, formatMonth, monthGrid, todayInOslo } from "@/lib/format";
import type { BookingDayCount } from "@/lib/types";
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
export default function YearGrid({ year, counts, dayHref, monthHref }: {
    year: string;
    counts: BookingDayCount[];
    dayHref: (date: string) => string;
    monthHref: (date: string) => string;
}) {
    const today = todayInOslo();
    const byDay = new Map(counts.map((c) => [c.date, c.count]));
    return (
        <>
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
                                              className={`${calendar.yearDay} ${level(count)} ${date === today ? calendar.yearToday : ""}`}
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
            <ul className={calendar.heatLegend} aria-label="Forklaring">
                <li><span className={calendar.heat1}/> 1–2</li>
                <li><span className={calendar.heat2}/> 3–5</li>
                <li><span className={calendar.heat3}/> 6–9</li>
                <li><span className={calendar.heat4}/> 10+ timer</li>
            </ul>
        </>
    );
}
