import Link from "next/link";
import { clockMinutes, formatTime, osloDay, osloMinutes, weekdayOf } from "@/lib/format";
import type { OpeningHours } from "@/lib/types";
import PersonsIcon, { personsFor } from "@/components/PersonsIcon/PersonsIcon";
import { customerLabel } from "@/lib/bookingStatus";
import PendingBadge, { OkStamp } from "../PendingBadge";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";
import { type Column, type Href, type BlockHref, blockLabel, blockHoursOn } from "./calendarLayout";

// Phone: the visits of each day as a list of cards; a tap opens the details window.
// always = also on a big screen ("Plan")
// Pause, blocked time and visits are shown in time order, so the day reads from top to bottom
export default function PhoneList({ columns, hrefFor, blockHref, always = false, openingHours = [], pauseHref }: {
    columns: Column[]; hrefFor: Href; blockHref: BlockHref; always?: boolean;
    openingHours?: OpeningHours[]; pauseHref?: (column: Column) => string;
}) {
    const pauseOn = (column: Column) => pauseHref
        ? openingHours.find((h) => h.dayOfWeek === weekdayOf(column.day) && h.breakStart && h.breakEnd)
        : undefined;
    const days = columns.filter((c) => c.bookings.length > 0 || c.blocks.length > 0 || pauseOn(c));
    return (
        <div className={always ? `${calendar.phoneList} ${calendar.planList}` : calendar.phoneList}>
            {days.length === 0 && <p className={styles.muted}>Ingen bestillinger.</p>}
            {days.map((column) => (
                <section key={column.id}>
                    <h3 className={calendar.phoneDay}>{column.title}</h3>
                    <ul className={styles.list}>
                        {(() => {
                            const pause = pauseOn(column);
                            const items: { minute: number; node: React.ReactNode }[] = [
                                ...column.blocks.map((block) => ({
                                    minute: osloDay(block.startUtc) < column.day ? 0 : osloMinutes(block.startUtc),
                                    node: (
                                        <li key={block.id}>
                                            <Link href={blockHref(block)} scroll={false} className={`${calendar.phoneCard} ${calendar.phoneBlocked}`}>
                                                <span className={calendar.time}>{blockHoursOn(block, column.day)}</span>
                                                <span>{blockLabel(block, true)}</span>
                                            </Link>
                                        </li>
                                    ),
                                })),
                                ...(pause && pauseHref ? [{
                                    minute: clockMinutes(pause.breakStart!),
                                    node: (
                                        <li key="pause">
                                            <Link href={pauseHref(column)} scroll={false} className={`${calendar.phoneCard} ${calendar.phoneBlocked}`}>
                                                <span className={calendar.time}>{pause.breakStart!.slice(0, 5)}–{pause.breakEnd!.slice(0, 5)}</span>
                                                <span>Pause</span>
                                            </Link>
                                        </li>
                                    ),
                                }] : []),
                                ...column.bookings.map((booking) => ({ minute: osloMinutes(booking.startUtc), node: (
                            <li key={booking.id}>
                                <Link href={hrefFor(booking)} scroll={false}
                                      className={`${calendar.phoneCard} ${calendar[booking.status] ?? ""}`}>
                                    <span className={calendar.cardTime}>
                                        <strong>{formatTime(booking.startUtc)}</strong>
                                        <small>{formatTime(booking.endUtc)}</small>
                                        {/* Who works: under the time, as a small pill with the initial */}
                                        <span className={calendar.cardEmployee} title={`Ansatt: ${booking.employeeName}`}>
                                            <span className={calendar.avatar} aria-hidden="true">{initials(booking.employeeName)}</span>
                                            <span className={calendar.srOnly}>Ansatt: </span>{booking.employeeName.split(" ")[0]}
                                        </span>
                                    </span>
                                    <span className={calendar.cardBody}>
                                        <span className={calendar.cardWho}><PersonsIcon persons={personsFor(booking)}/>{customerLabel(booking)}</span>
                                        <span className={calendar.service}>{booking.serviceName}</span>
                                    </span>
                                    <span className={calendar.cardStamp}>
                                        {booking.status === "Bekreftet" ? <OkStamp/>
                                            : <PendingBadge status={booking.status}/>}
                                    </span>
                                </Link>
                            </li>
                                ) })),
                            ];
                            return items.sort((a, b) => a.minute - b.minute).map((item) => item.node);
                        })()}
                    </ul>
                </section>
            ))}
        </div>
    );
}

// "Ida Hansen" -> "IH", "Maja" -> "M": first letter of the first and the last name the owner wrote in Ansatte
function initials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) {
        return "?";
    }
    const first = parts[0].charAt(0);
    return (parts.length > 1 ? first + parts[parts.length - 1].charAt(0) : first).toUpperCase();
}
