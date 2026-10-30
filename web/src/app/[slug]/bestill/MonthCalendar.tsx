import NextLink from "next/link";
import {formatDay, formatMonth, shiftDay} from "@/lib/format";
import type {FreeDay} from "@/lib/types";
import styles from "./MonthCalendar.module.css";

const WEEKDAYS = ["man.", "tir.", "ons.", "tor.", "fre.", "lør.", "søn."];

// "Velg dag": the whole month at once, so a customer can book weeks ahead without clicking week by week.
// Days with free times glow; closed, full and past days are dimmed. Days of the next month link to that month.
// A small pink crystal marks days with the customer's own visits (at any business)
// gridStart is the Monday before the 1st and gridEnd the Sunday after the last day
export default function MonthCalendar({month, gridStart, gridEnd, days, today, selectedDay, visitDays, dayHref, previousHref, nextHref}: {
    month: string;
    gridStart: string;
    gridEnd: string;
    days: FreeDay[];
    today: string;
    selectedDay: string | undefined;
    visitDays: Set<string>; // days the customer already has a visit somewhere (a small crystal in the corner)
    dayHref: (day: string) => string;
    previousHref: string | undefined;
    nextHref: string | undefined;
}) {
    const freeByDay = new Map(days.map((d) => [d.date, d.slots.length]));
    const dates: string[] = [];
    for (let day = gridStart; day <= gridEnd; day = shiftDay(day, 1)) {
        dates.push(day);
    }

    return (
        <section className={styles.calendar} aria-label={`Ledige dager i ${formatMonth(month)}`}>
            <header className={styles.head}>
                {previousHref ? (
                    <NextLink href={previousHref} scroll={false} className={styles.arrow}
                              aria-label="Forrige måned">‹</NextLink>
                ) : <span className={`${styles.arrow} ${styles.arrowOff}`} aria-hidden="true">‹</span>}
                <h2 className={styles.month}>{formatMonth(month)}</h2>
                {nextHref ? (
                    <NextLink href={nextHref} scroll={false} className={styles.arrow}
                              aria-label="Neste måned">›</NextLink>
                ) : <span className={`${styles.arrow} ${styles.arrowOff}`} aria-hidden="true">›</span>}
            </header>

            <div className={styles.grid}>
                {WEEKDAYS.map((name) => (
                    <span key={name} className={styles.weekday}>{name}</span>
                ))}
                {dates.map((day) => {
                    const free = freeByDay.get(day) ?? 0;
                    const mine = visitDays.has(day);
                    const gem = mine && <span className={styles.gem} aria-hidden="true"/>;
                    const mineLabel = mine ? ", du har time" : "";
                    const classes = [
                        styles.day,
                        day.startsWith(month) ? "" : styles.otherMonth,
                        day === today ? styles.today : "",
                        day === selectedDay ? styles.selected : "",
                    ].join(" ");
                    if (free === 0 || day < today) {
                        return (
                            <span key={day} className={`${classes} ${styles.full}`}
                                  aria-label={`${formatDay(day)}, ingen ledige${mineLabel}`}>
                                {gem}
                                <span className={styles.number}>{Number(day.slice(8))}</span>
                            </span>
                        );
                    }
                    return (
                        <NextLink key={day} href={dayHref(day)} scroll={false} className={classes}
                                  aria-current={day === selectedDay ? "date" : undefined}
                                  aria-label={`${formatDay(day)}, ${free} ledige${mineLabel}`}>
                            {gem}
                            <span className={styles.number}>{Number(day.slice(8))}</span>
                            <span className={styles.count}>{free} ledige</span>
                            <span className={styles.dot} aria-hidden="true"/>
                        </NextLink>
                    );
                })}
            </div>
        </section>
    );
}
