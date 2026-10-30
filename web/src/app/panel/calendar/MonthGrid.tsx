import Link from "next/link";
import { formatDay, formatTime, todayInOslo, weekdayOf } from "@/lib/format";
import { customerLabel } from "@/lib/bookingStatus";
import type { BusinessDetails } from "@/lib/types";
import PendingBadge from "../PendingBadge";
import calendar from "../calendar.module.css";
import { type Column, type Href, blockLabel, blockHoursOn } from "./calendarLayout";
import { isoWeek } from "./calendarDates";

// Month: a square per day with the number of visits, the first times and closed / blocked days;
// a click on the day number opens that day
const MONTH_PREVIEW = 3;

export default function MonthGrid({ days, month, business, dayHref, newHref, hrefFor }: {
    days: Column[]; month: string; business: BusinessDetails; dayHref: (date: string) => string;
    newHref: (date: string) => string; hrefFor: Href;
}) {
    const today = todayInOslo();
    const weeks: Column[][] = [];
    for (let i = 0; i < days.length; i += 7) {
        weeks.push(days.slice(i, i + 7));
    }
    return (
        <div className={calendar.monthWrap}>
            <div className={calendar.month}>
                <div className={calendar.corner}/>
                {["man.", "tir.", "ons.", "tor.", "fre.", "lør.", "søn."].map((name) => (
                    <div key={name} className={calendar.columnHead}>{name}</div>
                ))}
                {weeks.map((weekDays) => [
                    <div key={`uke-${weekDays[0].day}`} className={calendar.weekNumber}>{isoWeek(weekDays[0].day)}</div>,
                    ...weekDays.map((column) => {
                        const visits = [...column.bookings].sort((a, b) => a.startUtc.localeCompare(b.startUtc));
                        const closedAllDay = column.blocks.some((t) => !t.employeeId && blockHoursOn(t, column.day) === "Hele dagen");
                        const closed = closedAllDay || !business.openingHours.some((h) => h.dayOfWeek === weekdayOf(column.day));
                        const classes = [
                            calendar.monthDay,
                            column.day.startsWith(month) ? "" : calendar.otherMonth,
                            closed ? calendar.closedDay : "",
                            column.day === today ? calendar.today : "",
                        ].join(" ");
                        return (
                            <div key={column.day} className={classes}>
                                <div className={calendar.monthDayHead}>
                                    <Link href={dayHref(column.day)} className={calendar.dayNumber}
                                          aria-label={formatDay(column.day)}>{Number(column.day.slice(8))}</Link>
                                    <Link href={newHref(column.day)} scroll={false} className={calendar.monthAdd}
                                          aria-label={`Ny bestilling ${formatDay(column.day)}`}>+</Link>
                                </div>
                                {closed && <span className={calendar.closedLabel}>Stengt</span>}
                                {column.blocks.filter((t) => !closedAllDay || t.employeeId).map((block) => (
                                    <span key={block.id} className={calendar.monthBlock}>{blockLabel(block, true)}</span>
                                ))}
                                {visits.slice(0, MONTH_PREVIEW).map((booking) => (
                                    <Link key={booking.id} href={hrefFor(booking)} scroll={false}
                                          className={`${calendar.monthVisit} ${calendar[booking.status] ?? ""}`}
                                          title={`${booking.customerName} – ${booking.serviceName} hos ${booking.employeeName}`}>
                                        <PendingBadge status={booking.status} iconOnly/>
                                        {formatTime(booking.startUtc)} {customerLabel(booking)}
                                    </Link>
                                ))}
                                {visits.length > MONTH_PREVIEW && (
                                    <Link href={dayHref(column.day)} className={calendar.monthMore}>
                                        +{visits.length - MONTH_PREVIEW} til
                                    </Link>
                                )}
                            </div>
                        );
                    }),
                ])}
            </div>
        </div>
    );
}
