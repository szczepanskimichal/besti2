import Link from "next/link";
import { formatDay, formatMonth, formatTime, monthGrid, osloDay, shiftDay, shiftMonth } from "@/lib/format";
import { statusLabels } from "@/lib/bookingStatus";
import type { CustomerBooking, WaitlistEntry } from "@/lib/types";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import { daysBetween, isoWeek, mondayOf, shortDay, weekDays } from "../panel/calendar/calendarDates";
import Countdown from "./Countdown";
import styles from "./MyCalendar.module.css";

export type CalendarView = "ar" | "maned" | "uke" | "dag" | "plan";

// "Tid for ny time?": the day Besti suggests booking the same service again (last visit + N weeks)
export type Suggestion = {
    date: string;
    serviceName: string;
    businessName: string;
    bookHref: string;
};

const VIEWS: { view: CalendarView; label: string }[] = [
    { view: "ar", label: "År" },
    { view: "maned", label: "Måned" },
    { view: "uke", label: "Uke" },
    { view: "dag", label: "Dag" },
    { view: "plan", label: "Plan" },
];
const WEEKDAYS = ["man.", "tir.", "ons.", "tor.", "fre.", "lør.", "søn."];
// Visits written out in one month square; the rest is "+N"
const MONTH_CHIPS = 2;

const shortService = (name: string) => (name.length > 14 ? `${name.slice(0, 13)}…` : name);

// "Min kalender": every visit of the customer at every business, like the calendar on the phone
// (year, month, week, day and a plan of everything coming), so they see when they are free before booking.
// Colour = status, a bell = venteliste, a dashed ring = the day Besti suggests the next visit
export default function MyCalendar({ view, day, today, visits, waitlist, suggestions, link }: {
    view: CalendarView;
    day: string; // the chosen day; the year, month and week views show the period around it
    today: string;
    visits: CustomerBooking[]; // not cancelled, soonest first
    waitlist: WaitlistEntry[];
    suggestions: Suggestion[];
    link: (view: CalendarView, day: string) => string;
}) {
    const nowUtc = new Date().toISOString();
    const byDay = new Map<string, CustomerBooking[]>();
    for (const b of visits) {
        const date = osloDay(b.startUtc);
        byDay.set(date, [...(byDay.get(date) ?? []), b]);
    }
    const visitsOn = (date: string) => byDay.get(date) ?? [];
    const waitingOn = (date: string) => waitlist.filter((w) => w.date === date);
    const suggestedOn = (date: string) => suggestions.filter((s) => s.date === date);
    const statusClass = (b: CustomerBooking) => (b.endUtc < nowUtc ? styles.past : styles[b.status] ?? "");

    const month = day.slice(0, 7);
    const monday = mondayOf(day);
    // Where the arrows go and what the title says, per view
    const step: Record<CalendarView, [string, string]> = {
        ar: [`${Number(day.slice(0, 4)) - 1}${day.slice(4)}`, `${Number(day.slice(0, 4)) + 1}${day.slice(4)}`],
        maned: [`${shiftMonth(month, -1)}-01`, `${shiftMonth(month, 1)}-01`],
        uke: [shiftDay(day, -7), shiftDay(day, 7)],
        dag: [shiftDay(day, -1), shiftDay(day, 1)],
        plan: [day, day],
    };
    const title: Record<CalendarView, string> = {
        ar: day.slice(0, 4),
        maned: formatMonth(month),
        uke: `Uke ${isoWeek(monday)}`,
        dag: day === today ? `I dag, ${formatDay(day)}` : formatDay(day),
        plan: "Alt som kommer",
    };
    const next = visits.find((b) => b.startUtc > nowUtc);

    return (
        <section className={styles.wrap} aria-label="Min kalender">
            {next && (
                <Link href={link("dag", osloDay(next.startUtc))} scroll={false} className={styles.next}>
                    <span className={styles.nextLabel}>Neste time</span>
                    <strong className={styles.nextWhen}>{formatDay(osloDay(next.startUtc))} kl. {formatTime(next.startUtc)}</strong>
                    <span className={styles.nextWhat}>{next.serviceName} hos {next.businessName}</span>
                    <span><Countdown startUtc={next.startUtc}/></span>
                </Link>
            )}

            <nav className={styles.views} aria-label="Visning">
                {VIEWS.map((v) => (
                    <Link key={v.view} href={link(v.view, day)} scroll={false}
                          className={v.view === view ? `${styles.viewTab} ${styles.viewActive}` : styles.viewTab}
                          aria-current={v.view === view ? "page" : undefined}>{v.label}</Link>
                ))}
            </nav>

            <div className={styles.panel}>
                <header className={styles.head}>
                    {view !== "plan" ? (
                        <Link href={link(view, step[view][0])} scroll={false} className={styles.arrow}
                              aria-label="Forrige">‹</Link>
                    ) : <span/>}
                    <div className={styles.headText}>
                        <h2 className={styles.title}>{title[view]}</h2>
                        {view === "uke" && (
                            <p className={styles.sub}>{shortDay(monday)} – {shortDay(shiftDay(monday, 6))}</p>
                        )}
                        {day !== today && view !== "plan" && (
                            <Link href={link(view, today)} scroll={false} className={styles.todayLink}>I dag</Link>
                        )}
                    </div>
                    {view !== "plan" ? (
                        <Link href={link(view, step[view][1])} scroll={false} className={styles.arrow}
                              aria-label="Neste">›</Link>
                    ) : <span/>}
                </header>

                {view === "ar" && (
                    <div className={styles.year}>
                        {Array.from({ length: 12 }, (_, i) => `${day.slice(0, 4)}-${String(i + 1).padStart(2, "0")}`).map((m) => {
                            const { gridStart, gridEnd } = monthGrid(m);
                            const count = visits.filter((b) => osloDay(b.startUtc).startsWith(m)).length;
                            return (
                                <Link key={m} href={link("maned", `${m}-01`)} scroll={false}
                                      className={`${styles.mini} ${m === today.slice(0, 7) ? styles.miniNow : ""}`}>
                                    <span className={styles.miniName}>{formatMonth(m).split(" ")[0]}</span>
                                    <span className={styles.miniGrid} aria-hidden="true">
                                        {daysBetween(gridStart, gridEnd).map((d) => (
                                            <span key={d} className={[
                                                styles.miniDay,
                                                d.startsWith(m) ? "" : styles.miniOther,
                                                d.startsWith(m) && byDay.has(d) ? styles.miniVisit : "",
                                                d === today ? styles.miniToday : "",
                                            ].join(" ")}>{d.startsWith(m) ? Number(d.slice(8)) : ""}</span>
                                        ))}
                                    </span>
                                    <span className={styles.miniCount}>
                                        {count === 0 ? "Ingen timer" : `${count} ${count === 1 ? "time" : "timer"}`}
                                    </span>
                                </Link>
                            );
                        })}
                    </div>
                )}

                {view === "maned" && (
                    <>
                        <div className={styles.month}>
                            {WEEKDAYS.map((name) => <span key={name} className={styles.weekday}>{name}</span>)}
                            {(() => {
                                const { gridStart, gridEnd } = monthGrid(month);
                                return daysBetween(gridStart, gridEnd).map((d) => {
                                    const onDay = visitsOn(d);
                                    const suggested = suggestedOn(d).length > 0;
                                    return (
                                        <Link key={d} href={link("dag", d)} scroll={false}
                                              aria-label={`${formatDay(d)}${onDay.length ? `, ${onDay.length} ${onDay.length === 1 ? "time" : "timer"}` : ""}`}
                                              className={[
                                                  styles.cell,
                                                  d.startsWith(month) ? "" : styles.otherMonth,
                                                  d < today ? styles.pastDay : "",
                                                  suggested && onDay.length === 0 ? styles.suggestedDay : "",
                                              ].join(" ")}>
                                            <span className={d === today ? `${styles.number} ${styles.todayNumber}` : styles.number}>
                                                {Number(d.slice(8))}
                                            </span>
                                            {onDay.slice(0, MONTH_CHIPS).map((b) => (
                                                <span key={b.id} className={`${styles.chip} ${statusClass(b)}`}>
                                                    {formatTime(b.startUtc)}
                                                    <span className={styles.chipName}> {shortService(b.serviceName)}</span>
                                                </span>
                                            ))}
                                            {onDay.length > MONTH_CHIPS && (
                                                <span className={styles.chipMore}>+{onDay.length - MONTH_CHIPS}</span>
                                            )}
                                            {waitingOn(d).length > 0 && (
                                                <span className={styles.cellBell}><CrystalIcon name="bjelle"/></span>
                                            )}
                                        </Link>
                                    );
                                });
                            })()}
                        </div>
                        <ul className={styles.legend} aria-label="Forklaring">
                            <li><span className={`${styles.swatch} ${styles.Bekreftet}`}/> Bekreftet</li>
                            <li><span className={`${styles.swatch} ${styles.Venter}`}/> Venter</li>
                            <li><span className={`${styles.swatch} ${styles.VenterKunde}`}/> Svar ønskes</li>
                            <li><span className={styles.ring}/> Tid for ny time?</li>
                        </ul>
                    </>
                )}

                {view === "uke" && (
                    <ol className={styles.week}>
                        {weekDays(monday).map((d) => {
                            const onDay = visitsOn(d);
                            const extra = waitingOn(d).length + suggestedOn(d).length;
                            return (
                                <li key={d}>
                                    <Link href={link("dag", d)} scroll={false}
                                          className={`${styles.weekRow} ${d === today ? styles.weekToday : ""} ${d < today ? styles.pastDay : ""}`}>
                                        <span className={styles.weekDate}>
                                            <span className={styles.weekName}>{WEEKDAYS[(new Date(`${d}T12:00:00Z`).getUTCDay() + 6) % 7]}</span>
                                            <span className={styles.weekNumber}>{Number(d.slice(8))}</span>
                                        </span>
                                        <span className={styles.weekItems}>
                                            {onDay.length === 0 && extra === 0 && <span className={styles.freeText}>Ledig</span>}
                                            {onDay.map((b) => (
                                                <span key={b.id} className={`${styles.chip} ${styles.chipWide} ${statusClass(b)}`}>
                                                    <strong>{formatTime(b.startUtc)}</strong> {b.serviceName} hos {b.businessName}
                                                </span>
                                            ))}
                                            {waitingOn(d).map((w) => (
                                                <span key={w.id} className={styles.weekNote}><CrystalIcon name="bjelle"/> Venteliste: {w.serviceName}</span>
                                            ))}
                                            {suggestedOn(d).map((s) => (
                                                <span key={s.serviceName + s.businessName} className={styles.weekNote}>
                                                    <span className={styles.ring}/> Tid for ny {s.serviceName.toLowerCase()}?
                                                </span>
                                            ))}
                                        </span>
                                    </Link>
                                </li>
                            );
                        })}
                    </ol>
                )}

                {view === "dag" && (
                    <DayPlan date={day} today={today} nowUtc={nowUtc} visits={visitsOn(day)} waitlist={waitingOn(day)}
                             suggestions={suggestedOn(day)}/>
                )}

                {view === "plan" && (() => {
                    const coming = [...new Set([
                        ...visits.filter((b) => b.endUtc >= nowUtc).map((b) => osloDay(b.startUtc)),
                        ...waitlist.map((w) => w.date),
                        ...suggestions.map((s) => s.date),
                    ])].filter((d) => d >= today).sort();
                    return coming.length === 0 ? (
                        <div className={styles.free}>
                            <p className={styles.freeTitle}>Ingenting planlagt ennå ✨</p>
                            <p className={styles.muted}>Lyst til å unne deg noe? <Link href="/">Finn en bedrift</Link></p>
                        </div>
                    ) : (
                        <ol className={styles.plan}>
                            {coming.map((d) => (
                                <li key={d}>
                                    <Link href={link("dag", d)} scroll={false} className={styles.planDay}>
                                        {d === today ? `I dag, ${formatDay(d)}` : formatDay(d)}
                                    </Link>
                                    <DayPlan date={d} today={today} nowUtc={nowUtc} visits={visitsOn(d)}
                                             waitlist={waitingOn(d)} suggestions={suggestedOn(d)} compact/>
                                </li>
                            ))}
                        </ol>
                    );
                })()}
            </div>
        </section>
    );
}

// One day: every visit as a card on a time line, then the venteliste and Besti's suggestion
function DayPlan({ date, today, nowUtc, visits, waitlist, suggestions, compact = false }: {
    date: string; today: string; nowUtc: string; visits: CustomerBooking[]; waitlist: WaitlistEntry[];
    suggestions: Suggestion[]; compact?: boolean;
}) {
    if (visits.length === 0 && waitlist.length === 0 && suggestions.length === 0) {
        return (
            <div className={styles.free}>
                <p className={styles.freeTitle}>{date < today ? "Ingen timer denne dagen." : "Helt ledig dag ✨"}</p>
                {date >= today && <p className={styles.muted}>Lyst til å unne deg noe? <Link href="/">Finn en bedrift</Link></p>}
            </div>
        );
    }
    return (
        <ol className={compact ? `${styles.timeline} ${styles.timelineCompact}` : styles.timeline}>
            {visits.map((b) => {
                const over = b.endUtc < nowUtc;
                return (
                    <li key={b.id} className={`${styles.item} ${over ? styles.past : styles[b.status] ?? ""}`}>
                        <span className={styles.itemTime}>
                            <strong>{formatTime(b.startUtc)}</strong>
                            <span>{formatTime(b.endUtc)}</span>
                        </span>
                        <div className={styles.itemBody}>
                            <p className={styles.itemTitle}>{b.serviceName}</p>
                            <p className={styles.muted}>
                                hos {b.employeeName}, <Link href={`/${b.businessSlug}`}>{b.businessName}</Link>
                            </p>
                            {over ? (
                                <p className={styles.links}>
                                    <Link href={`/${b.businessSlug}/bestill?${new URLSearchParams({ tjeneste: b.serviceId, ansatt: b.employeeId })}`}>
                                        Bestill igjen
                                    </Link>
                                    {!b.isReviewed && <> · <Link href="/min-besti/vurderinger">Gi vurdering</Link></>}
                                </p>
                            ) : (
                                <>
                                    <span className={styles.status}>
                                        {b.status === "VenterKunde" ? "Svar ønskes" : statusLabels[b.status] ?? b.status}
                                    </span>
                                    {!compact && <p className={styles.countdown}><Countdown startUtc={b.startUtc}/></p>}
                                    <p className={styles.links}>
                                        <a href={`https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(b.businessAddress)}`}
                                           target="_blank" rel="noopener noreferrer">Veibeskrivelse</a>
                                        {" · "}
                                        <a href={`/min-besti/kalender/${b.id}`}>Legg til i kalender</a>
                                        {" · "}
                                        <a href={`#time-${b.id}`}>Endre eller avbestill</a>
                                    </p>
                                </>
                            )}
                        </div>
                    </li>
                );
            })}
            {waitlist.map((w) => (
                <li key={w.id} className={`${styles.item} ${styles.itemNote}`}>
                    <span className={styles.itemTime}><CrystalIcon name="bjelle"/></span>
                    <div className={styles.itemBody}>
                        <p className={styles.itemTitle}>{w.notified ? "Det ble ledig!" : "På venteliste"}</p>
                        <p className={styles.muted}>{w.serviceName} hos {w.businessName}</p>
                        <p className={styles.links}>
                            <Link href={`/${w.businessSlug}/bestill?${new URLSearchParams({
                                tjeneste: w.serviceId, maned: w.date.slice(0, 7), dato: w.date,
                                ...(w.employeeId ? { ansatt: w.employeeId } : {}),
                            })}`}>Se ledige tider</Link>
                        </p>
                    </div>
                </li>
            ))}
            {suggestions.map((s) => (
                <li key={s.serviceName + s.businessName} className={`${styles.item} ${styles.itemSuggestion}`}>
                    <span className={styles.itemTime}><span className={styles.ring}/></span>
                    <div className={styles.itemBody}>
                        <p className={styles.itemTitle}>Tid for ny time?</p>
                        <p className={styles.muted}>{s.serviceName} hos {s.businessName}</p>
                        <p className={styles.links}><Link href={s.bookHref}>Finn en tid</Link></p>
                    </div>
                </li>
            ))}
        </ol>
    );
}
