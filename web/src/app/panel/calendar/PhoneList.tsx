import Link from "next/link";
import { formatTime } from "@/lib/format";
import PendingBadge from "../PendingBadge";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";
import { type Column, type Href, type BlockHref, blockLabel, blockHoursOn } from "./calendarLayout";

// Phone: the visits of each day as a list of cards; a tap opens the details window
export default function PhoneList({ columns, hrefFor, blockHref }: { columns: Column[]; hrefFor: Href; blockHref: BlockHref }) {
    const days = columns.filter((c) => c.bookings.length > 0 || c.blocks.length > 0);
    return (
        <div className={calendar.phoneList}>
            {days.length === 0 && <p className={styles.muted}>Ingen bestillinger.</p>}
            {days.map((column) => (
                <section key={column.id}>
                    <h3 className={calendar.phoneDay}>{column.title}</h3>
                    <ul className={styles.list}>
                        {column.blocks.map((block) => (
                            <li key={block.id}>
                                <Link href={blockHref(block)} scroll={false} className={`${calendar.phoneCard} ${calendar.phoneBlocked}`}>
                                    <span className={calendar.time}>{blockHoursOn(block, column.day)}</span>
                                    <span>{blockLabel(block, true)}</span>
                                </Link>
                            </li>
                        ))}
                        {[...column.bookings].sort((a, b) => a.startUtc.localeCompare(b.startUtc)).map((booking) => (
                            <li key={booking.id}>
                                <Link href={hrefFor(booking)} scroll={false}
                                      className={`${calendar.phoneCard} ${calendar[booking.status] ?? ""}`}>
                                    <span className={calendar.time}>
                                        {formatTime(booking.startUtc)}–{formatTime(booking.endUtc)}
                                        <PendingBadge status={booking.status}/>
                                    </span>
                                    <span>{booking.customerName}</span>
                                    <span className={calendar.service}>{booking.serviceName} hos {booking.employeeName}</span>
                                </Link>
                            </li>
                        ))}
                    </ul>
                </section>
            ))}
        </div>
    );
}
