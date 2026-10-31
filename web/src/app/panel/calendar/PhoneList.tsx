import Link from "next/link";
import { formatTime } from "@/lib/format";
import { customerLabel } from "@/lib/bookingStatus";
import PendingBadge, { OkStamp } from "../PendingBadge";
import styles from "../page.module.css";
import calendar from "../calendar.module.css";
import { type Column, type Href, type BlockHref, blockLabel, blockHoursOn } from "./calendarLayout";

// Phone: the visits of each day as a list of cards; a tap opens the details window.
// always = also on a big screen ("Plan")
export default function PhoneList({ columns, hrefFor, blockHref, always = false }: {
    columns: Column[]; hrefFor: Href; blockHref: BlockHref; always?: boolean;
}) {
    const days = columns.filter((c) => c.bookings.length > 0 || c.blocks.length > 0);
    return (
        <div className={always ? `${calendar.phoneList} ${calendar.planList}` : calendar.phoneList}>
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
                                    <span className={calendar.cardTime}>
                                        <strong>{formatTime(booking.startUtc)}</strong>
                                        <small>{formatTime(booking.endUtc)}</small>
                                    </span>
                                    <span className={calendar.cardBody}>
                                        <span className={calendar.cardWho}>{customerLabel(booking)}</span>
                                        <span className={calendar.service}>{booking.serviceName}</span>
                                        <span className={calendar.cardEmployee}>
                                            <span className={calendar.avatar} aria-hidden="true">{booking.employeeName.charAt(0)}</span>
                                            {booking.employeeName}
                                        </span>
                                    </span>
                                    <span className={calendar.cardStamp}>
                                        {booking.status === "Bekreftet" ? <OkStamp/>
                                            : <PendingBadge status={booking.status}/>}
                                    </span>
                                </Link>
                            </li>
                        ))}
                    </ul>
                </section>
            ))}
        </div>
    );
}
