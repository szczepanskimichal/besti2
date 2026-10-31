import Link from "next/link";
import { clockMinutes, formatTime, osloMinutes, weekdayOf } from "@/lib/format";
import type { OpeningHours } from "@/lib/types";
import { customerLabel, statusLabels } from "@/lib/bookingStatus";
import PendingBadge, { OkStamp } from "../PendingBadge";
import calendar from "../calendar.module.css";
import { PX_PER_MINUTE, type Column, type Range, type Href, type BlockHref, blockMinutes, blockLabel, describeBlock, hourMarks, halfHours, clock } from "./calendarLayout";

// The grid: hours on the left, one column per employee (day) or per day (week)
export default function TimeGrid({ columns, range, showEmployee, hrefFor, blockHref, newHref, openingHours = [] }: {
    columns: Column[]; range: Range; showEmployee: boolean; hrefFor: Href; blockHref: BlockHref;
    newHref: (column: Column, minute: number) => string; openingHours?: OpeningHours[];
}) {
    const height = (range.end - range.start) * PX_PER_MINUTE;
    const marks = hourMarks(range);
    return (
        <div className={calendar.scroller}>
            <div className={calendar.grid} style={{ gridTemplateColumns: `64px repeat(${columns.length}, minmax(140px, 1fr))` }}>
                <div className={calendar.corner}/>
                {columns.map((column) => (
                    <div key={column.id} className={calendar.columnHead}>{column.title}</div>
                ))}

                <div className={calendar.hours} style={{ height }}>
                    {marks.map((minute) => (
                        <span key={minute} style={{ top: (minute - range.start) * PX_PER_MINUTE }}>
                            {String(minute / 60).padStart(2, "0")}:00
                        </span>
                    ))}
                </div>

                {columns.map((column) => (
                    <div key={column.id} className={calendar.column} style={{ height }}>
                        {marks.map((minute) => (
                            <div key={minute} className={calendar.hourLine} style={{ top: (minute - range.start) * PX_PER_MINUTE }}/>
                        ))}
                        {/* Empty half hours: a click opens "Ny bestilling" with this time (visits lie on top) */}
                        {halfHours(range).map((minute) => (
                            <Link key={minute} href={newHref(column, minute)} scroll={false} className={calendar.emptySlot}
                                  style={{ top: (minute - range.start) * PX_PER_MINUTE, height: 30 * PX_PER_MINUTE }}
                                  aria-label={`Ny bestilling ${column.title} kl. ${clock(minute)}`}/>
                        ))}
                        {/* The daily break (lunch): customers cannot book it; a click still reaches the slot below */}
                        {openingHours.filter((h) => h.dayOfWeek === weekdayOf(column.day) && h.breakStart && h.breakEnd)
                            .map((h) => {
                                const start = Math.max(clockMinutes(h.breakStart!), range.start);
                                const end = Math.min(clockMinutes(h.breakEnd!), range.end);
                                if (end <= start) return null;
                                return (
                                    <div key="pause" className={calendar.breakTime} aria-hidden="true"
                                         style={{ top: (start - range.start) * PX_PER_MINUTE, height: (end - start) * PX_PER_MINUTE }}>
                                        Pause
                                    </div>
                                );
                            })}
                        {/* Blocked time: grey, under the visits, over the empty slots */}
                        {column.blocks.map((block) => {
                            const { start, end } = blockMinutes(block, column.day, range);
                            if (end <= start) return null;
                            return (
                                <Link key={block.id} href={blockHref(block)} scroll={false} className={calendar.blocked}
                                      style={{ top: (start - range.start) * PX_PER_MINUTE, height: (end - start) * PX_PER_MINUTE }}
                                      title={`${blockLabel(block, showEmployee)} – ${describeBlock(block)}`}>
                                    {blockLabel(block, showEmployee)}
                                </Link>
                            );
                        })}
                        {column.bookings.map((booking) => {
                            const start = Math.max(osloMinutes(booking.startUtc), range.start);
                            const end = Math.min(osloMinutes(booking.endUtc) || 24 * 60, range.end);
                            return (
                                <Link
                                    key={booking.id}
                                    href={hrefFor(booking)}
                                    scroll={false}
                                    className={`${calendar.booking} ${calendar[booking.status] ?? ""} ${(end - start) * PX_PER_MINUTE < 80 ? calendar.shortVisit : ""}`}
                                    style={{
                                        top: (start - range.start) * PX_PER_MINUTE,
                                        height: Math.max(end - start, 15) * PX_PER_MINUTE,
                                        // The round stamp fits inside the visit: smaller on short visits
                                        ["--stamp" as string]: `${Math.min(84, Math.max(end - start, 15) * PX_PER_MINUTE - 12)}px`,
                                    }}
                                    title={`${booking.customerName} – ${booking.serviceName} hos ${booking.employeeName} (${statusLabels[booking.status] ?? booking.status})`}
                                >
                                    <span className={calendar.time}>
                                        {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                    </span>
                                    {booking.status === "Bekreftet" ? <OkStamp/> : <PendingBadge status={booking.status}/>}
                                    <span className={calendar.customer}>{customerLabel(booking)}</span>
                                    <span className={calendar.service}>
                                        {showEmployee ? `${booking.employeeName} · ${booking.serviceName}` : booking.serviceName}
                                    </span>
                                </Link>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
}
