import Link from "next/link";
import { clockMinutes, formatTime, osloMinutes, weekdayOf } from "@/lib/format";
import type { OpeningHours } from "@/lib/types";
import PersonsIcon, { personsFor } from "@/components/PersonsIcon/PersonsIcon";
import { customerLabel, statusLabels } from "@/lib/bookingStatus";
import { MiniStamp } from "../PendingBadge";
import calendar from "../calendar.module.css";
import { PX_PER_MINUTE, type Column, type Range, type Href, type BlockHref, blockMinutes, blockLabel, describeBlock, blockHoursOn, hourMarks, halfHours, clock } from "./calendarLayout";

// Visits that overlap in one column lie side by side (each in its own lane) instead of on top of each other,
// so every name and time can be read
function lanesFor(bookings: Column["bookings"]): Map<string, { lane: number; lanes: number }> {
    const sorted = [...bookings].sort((a, b) => osloMinutes(a.startUtc) - osloMinutes(b.startUtc));
    const result = new Map<string, { lane: number; lanes: number }>();
    let group: { id: string; lane: number }[] = [];
    let laneEnds: number[] = [];
    let groupEnd = -1;
    const close = () => group.forEach((g) => result.set(g.id, { lane: g.lane, lanes: laneEnds.length }));
    for (const booking of sorted) {
        const start = osloMinutes(booking.startUtc);
        const end = osloMinutes(booking.endUtc) || 24 * 60;
        if (start >= groupEnd) {
            close();
            group = [];
            laneEnds = [];
        }
        let lane = laneEnds.findIndex((laneEnd) => laneEnd <= start);
        if (lane < 0) {
            lane = laneEnds.length;
            laneEnds.push(end);
        } else {
            laneEnds[lane] = end;
        }
        group.push({ id: booking.id, lane });
        groupEnd = Math.max(groupEnd, end);
    }
    close();
    return result;
}

// The grid: hours on the left, one column per employee (day) or per day (week)
export default function TimeGrid({ columns, range, showEmployee, hrefFor, blockHref, newHref, pauseHref, openingHours = [] }: {
    columns: Column[]; range: Range; showEmployee: boolean; hrefFor: Href; blockHref: BlockHref;
    newHref: (column: Column, minute: number) => string; pauseHref?: (column: Column) => string; openingHours?: OpeningHours[];
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

                {columns.map((column) => {
                    const lanes = lanesFor(column.bookings);
                    return (
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
                                const pauseStyle = { top: (start - range.start) * PX_PER_MINUTE, height: (end - start) * PX_PER_MINUTE };
                                const label = <span className={calendar.offLabel}>Pause<small>{h.breakStart!.slice(0, 5)}–{h.breakEnd!.slice(0, 5)}</small></span>;
                                return pauseHref ? (
                                    <Link key="pause" href={pauseHref(column)} scroll={false} className={calendar.breakTime} style={pauseStyle}
                                          aria-label={`Pause ${h.breakStart!.slice(0, 5)}–${h.breakEnd!.slice(0, 5)}, ${column.title}`}>
                                        {label}
                                    </Link>
                                ) : (
                                    <div key="pause" className={calendar.breakTime} aria-hidden="true" style={pauseStyle}>{label}</div>
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
                                    <span className={calendar.offLabel}>{blockLabel(block, showEmployee)}<small>{blockHoursOn(block, column.day)}</small></span>
                                </Link>
                            );
                        })}
                        {column.bookings.map((booking) => {
                            const { lane, lanes: laneCount } = lanes.get(booking.id) ?? { lane: 0, lanes: 1 };
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
                                        ...(laneCount > 1 ? {
                                            left: `calc(${(lane * 100) / laneCount}% + 3px)`,
                                            right: "auto",
                                            width: `calc(${100 / laneCount}% - 6px)`,
                                        } : {}),
                                    }}
                                    title={`${booking.customerName} – ${booking.serviceName} hos ${booking.employeeName} (${statusLabels[booking.status] ?? booking.status})`}
                                >
                                    <span className={calendar.time}>
                                        {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                        <MiniStamp status={booking.status}/>
                                    </span>
                                    <span className={calendar.customer}><PersonsIcon persons={personsFor(booking)}/>{customerLabel(booking)}</span>
                                    <span className={calendar.service}>
                                        {showEmployee ? `${booking.employeeName} · ${booking.serviceName}` : booking.serviceName}
                                    </span>
                                </Link>
                            );
                        })}
                    </div>
                    );
                })}
            </div>
        </div>
    );
}
