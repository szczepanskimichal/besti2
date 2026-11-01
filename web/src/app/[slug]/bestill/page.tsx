import NextLink from "next/link";
import {notFound} from "next/navigation";
import {getBusinessBySlug, getFreeDays, getMe, getMyBookings} from "@/lib/api";
import {getToken} from "@/lib/auth";
import {daysBetween, formatDay, osloDay, formatLength, formatMonth, formatPrice, formatTime, isIsoDate, isIsoMonth, monthGrid, shiftDay, shiftMonth, todayInOslo} from "@/lib/format";
import {freeCancelText} from "@/lib/freeCancel";
import AutoSubmitSelect from "@/components/AutoSubmitSelect/AutoSubmitSelect";
import BookingForm from "./BookingForm";
import InviteForm from "./InviteForm";
import VisitStamp from "./VisitStamp";
import PersonsIcon from "@/components/PersonsIcon/PersonsIcon";
import MonthCalendar from "./MonthCalendar";
import WaitlistForm from "./WaitlistForm";
import styles from "./page.module.css";

// The biggest group the API takes (InviteRules.MaxPersons)
const MAX_PERSONS = 8;
// How far ahead the month calendar goes
const MONTHS_AHEAD = 12;
// Venteliste only for the next 60 days (WaitlistService.MaxDaysAhead)
const WAITLIST_DAYS = 60;

type Props = {
    params: Promise<{ slug: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; maned?: string; fra?: string; dato?: string; tid?: string; antall?: string; sammen?: string; selv?: string; }>;
};

export default async function BookingPage({params, searchParams}: Props) {
    const {slug} = await params;
    const {tjeneste, ansatt, maned, fra, dato, tid, antall, sammen, selv} = await searchParams;

    const business = await getBusinessBySlug(slug);
    const service = business?.services.find((s) => s.id === tjeneste);

    if (!business || !service) {
        notFound();
    }

    // BESTI sammen: sammen=N books N visits at the same time, one employee each ("1" from old links = two).
    // Never more people than the business has employees
    const maxPersons = Math.min(MAX_PERSONS, business.employees.length);
    const persons = sammen && maxPersons >= 2 ? Math.min(Math.max(Math.floor(Number(sammen)) || 2, 2), maxPersons) : 1;
    const together = persons > 1;
    // selv=0: the inviter only sends (and maybe pays), e.g. the boss who sends the team to the spa
    const organizerJoins = selv !== "0";

    // Only values that still make sense (old "Bestill igjen" links, edited or cut-off addresses).
    // Without a visit of their own, the inviter has no employee to choose
    const employeeId = organizerJoins && business.employees.some((e) => e.id === ansatt) ? ansatt : undefined; // undefined = Hvem som helst
    // "Per time" services: the customer picks how many times the service length in a row, max 8 hours
    const maxUnits = service.priceType === "PerTime" ? Math.floor(480 / service.durationMinutes) : 1;
    const units = Math.min(Math.max(Math.floor(Number(antall)) || 1, 1), maxUnits);
    const today = todayInOslo();
    const chosenDay = isIsoDate(dato) && dato >= today ? dato : undefined;

    // Whole weeks of one month; past days are never free, so the API only counts from today
    async function loadMonth(inMonth: string) {
        const {gridStart, gridEnd} = monthGrid(inMonth);
        const from = gridStart < today ? today : gridStart;
        const days = await getFreeDays(service!.id, employeeId, from, units, persons, daysBetween(from, gridEnd) + 1);
        return {gridStart, gridEnd, days};
    }

    // The month in the calendar: from the link (fra = the week start in old links), never in the past
    // and never more than a year ahead
    const thisMonth = today.slice(0, 7);
    const lastMonth = shiftMonth(thisMonth, MONTHS_AHEAD);
    const wantedMonth = isIsoMonth(maned) ? maned : (chosenDay ?? (isIsoDate(fra) ? fra : today)).slice(0, 7);
    let month = wantedMonth < thisMonth ? thisMonth : wantedMonth > lastMonth ? lastMonth : wantedMonth;
    // Free times for the month and who is booking (null = not logged in), both at the same time
    const token = await getToken();
    // The customer's own visits (any business) are shown in the calendar, so they do not book on top of them
    const [firstMonth, me, myBookings] = await Promise.all([
        loadMonth(month),
        token ? getMe(token) : null,
        token ? getMyBookings(token).catch(() => null) : null,
    ]);
    let {gridStart, gridEnd, days} = firstMonth;
    // Late in the month nothing may be left: without a chosen month or day the calendar opens on the next one
    if (!isIsoMonth(maned) && !chosenDay && month === thisMonth
        && !days?.some((d) => d.date.startsWith(month) && d.slots.length > 0)) {
        month = shiftMonth(month, 1);
        ({gridStart, gridEnd, days} = await loadMonth(month));
    }
    const monthDays = days?.filter((d) => d.date.startsWith(month)) ?? [];
    // The chosen day, otherwise the first day of the month with free times
    const selected = monthDays.find((d) => d.date === chosenDay) ?? monthDays.find((d) => d.slots.length > 0);
    // Own visits from now on; the API refuses a booking on top of one (any business), so those times are greyed out.
    // Only when the customer gets a visit too (an inviter who only sends can be busy)
    const nowUtc = new Date().toISOString();
    const myVisits = me && !me.isBusiness && organizerJoins
        ? (myBookings ?? []).filter((b) => b.status !== "Avlyst" && b.endUtc > nowUtc)
            .sort((a, b) => a.startUtc.localeCompare(b.startUtc))
        : [];
    const visitDays = new Set(myVisits.map((b) => osloDay(b.startUtc)));
    const waitingDays = new Set(myVisits.filter((b) => b.status === "Venter" || b.status === "VenterKunde").map((b) => osloDay(b.startUtc)));
    const visitsOnSelected = myVisits.filter((b) => osloDay(b.startUtc) === selected?.date);
    const visitMs = units * service.durationMinutes * 60_000;
    function isMine(startUtc: string) {
        const end = new Date(Date.parse(startUtc) + visitMs).toISOString();
        return visitsOnSelected.some((b) => b.startUtc < end && b.endUtc > startUtc);
    }
    // The chosen time must still be free; otherwise the customer picks a new one
    const chosen = tid && !isMine(tid) ? selected?.slots.find((s) => s.startUtc === tid) : undefined;
    const chosenEmployee = business.employees.find((e) => e.id === chosen?.employeeId);
    // Venteliste: the days of this month the customer can still wait for
    const waitlistDays = monthDays.map((d) => d.date).filter((d) => d <= shiftDay(today, WAITLIST_DAYS));

    // Link to this page with another month, day or time (the employee choice and the group size stay)
    function bookingUrl(inMonth: string, day?: string, time?: string, group = persons) {
        const query = new URLSearchParams({tjeneste: service!.id, maned: inMonth});
        if (group > 1) {
            query.set("sammen", String(group));
            if (!organizerJoins) {
                query.set("selv", "0");
            }
        }
        if (employeeId) {
            query.set("ansatt", employeeId);
        }
        if (units > 1) {
            query.set("antall", String(units));
        }
        if (day) {
            query.set("dato", day);
        }
        if (time) {
            query.set("tid", time);
        }
        return `/${business!.slug}/bestill?${query}`;
    }

    const currentUrl = bookingUrl(month, selected?.date, chosen?.startUtc);

    return (
        <main className={styles.main}>
            <NextLink href={`/${business.slug}`} className={styles.back}>← Tilbake til {business.name}</NextLink>

            <h1 className={styles.title}>Bestill {service.name}</h1>
            <p className={styles.muted}>
                {formatLength(service.durationMinutes)} · {formatPrice(service.priceNok, service.priceType)}
            </p>

            {/* BESTI sammen: book alone or invite friends to the same time (the chosen day stays) */}
            {maxPersons >= 2 && (
                <nav className={styles.together} aria-label="Hvem bestiller du for?">
                    <NextLink href={bookingUrl(month, selected?.date, undefined, 1)} scroll={false}
                              className={together ? styles.togetherOption : `${styles.togetherOption} ${styles.togetherSelected}`}
                              aria-current={together ? undefined : "page"}>
                        Bare meg
                    </NextLink>
                    <NextLink href={bookingUrl(month, selected?.date, undefined, together ? persons : 2)} scroll={false}
                              className={together ? `${styles.togetherOption} ${styles.togetherSelected}` : styles.togetherOption}
                              aria-current={together ? "page" : undefined}>
                        Sammen <PersonsIcon persons={2}/>
                    </NextLink>
                </nav>
            )}
            {together && (
                <p className={styles.muted}>
                    Bestill med din BESTI, eller med hele gjengen: velg en tid der alle får hver sin ansatt samtidig.
                    De andre får én lenke og svarer med ett trykk. Ingenting bestilles før noen sier ja.
                </p>
            )}

            <form method="get" className={styles.filters}>
                <input type="hidden" name="tjeneste" value={service.id}/>
                <input type="hidden" name="maned" value={month}/>
                {selected && <input type="hidden" name="dato" value={selected.date}/>}

                {together && (
                    <>
                        <label className={styles.field}>
                            Hvor mange timer?
                            <AutoSubmitSelect name="sammen" defaultValue={persons}>
                                {Array.from({length: maxPersons - 1}, (_, i) => i + 2).map((count) => (
                                    <option key={count} value={count}>{count} personer</option>
                                ))}
                            </AutoSubmitSelect>
                        </label>
                        <label className={styles.field}>
                            Skal du selv ha time?
                            <AutoSubmitSelect name="selv" defaultValue={organizerJoins ? "1" : "0"}>
                                <option value="1">Ja, jeg er med</option>
                                <option value="0">Nei, jeg bare inviterer</option>
                            </AutoSubmitSelect>
                        </label>
                    </>
                )}

                {organizerJoins && (
                    <label className={styles.field}>
                        {together ? "Ansatt for deg" : "Ansatt"}
                        <AutoSubmitSelect name="ansatt" defaultValue={employeeId ?? ""}>
                            <option value="">Hvem som helst</option>
                            {business.employees.map((employee) => (
                                <option key={employee.id} value={employee.id}>
                                    {employee.name} {employee.lastName}
                                </option>
                            ))}
                        </AutoSubmitSelect>
                    </label>
                )}

                {maxUnits > 1 && (
                    <label className={styles.field}>
                        Varighet
                        <AutoSubmitSelect name="antall" defaultValue={units}>
                        {Array.from({length: maxUnits}, (_, i) => i + 1).map((count) => (
                                <option key={count} value={count}>{formatLength(count * service.durationMinutes)}</option>
                            ))}
                        </AutoSubmitSelect>
                    </label>
                )}

                <button type="submit" className={styles.secondaryButton}>Vis ledige tider</button>
            </form>

            <h2 className={styles.sectionTitle}>Velg dag</h2>
            <div className={styles.picker}>
                <MonthCalendar month={month} gridStart={gridStart} gridEnd={gridEnd} days={days ?? []} today={today}
                               selectedDay={selected?.date} visitDays={visitDays} waitingDays={waitingDays} dayHref={(day) => bookingUrl(day.slice(0, 7), day)}
                               previousHref={month > thisMonth ? bookingUrl(shiftMonth(month, -1)) : undefined}
                               nextHref={month < lastMonth ? bookingUrl(shiftMonth(month, 1)) : undefined}/>

                <div className={styles.times}>
                    <h2 className={styles.timesTitle}>
                        {selected ? `Ledige tider ${formatDay(selected.date)}` : "Ledige tider"}
                    </h2>
                    {!selected ? (
                        <p className={styles.muted}>
                            {together
                                ? `Ingen tider for ${persons} samtidig i ${formatMonth(month)}. Prøv neste måned, færre personer eller «Hvem som helst».`
                                : `Ingen ledige tider i ${formatMonth(month)}.`}
                            {month < lastMonth && (
                                <>
                                    {" "}<NextLink href={bookingUrl(shiftMonth(month, 1))} scroll={false}>Se {formatMonth(shiftMonth(month, 1))} →</NextLink>
                                </>
                            )}
                        </p>
                    ) : (
                        <ul className={styles.slots}>
                            {selected.slots.map((slot) => isMine(slot.startUtc) ? (
                                <li key={slot.startUtc}>
                                    <span className={`${styles.slot} ${styles.slotMine}`}
                                          title="Du har en annen time da">
                                        {formatTime(slot.startUtc)}
                                    </span>
                                </li>
                            ) : (
                                <li key={slot.startUtc}>
                                    <NextLink
                                        href={bookingUrl(month, selected.date, slot.startUtc)}
                                        className={slot.startUtc === chosen?.startUtc ? `${styles.slot} ${styles.slotSelected}` : styles.slot}
                                        aria-current={slot.startUtc === chosen?.startUtc ? "true" : undefined}>
                                        {formatTime(slot.startUtc)}
                                    </NextLink>
                                </li>
                            ))}
                        </ul>
                    )}
                    {visitsOnSelected.length > 0 && (
                        <div className={visitsOnSelected.every((b) => b.status === "Venter" || b.status === "VenterKunde") ? `${styles.myVisits} ${styles.myVisitsWait}` : styles.myVisits}>
                            <p className={styles.myVisitsTitle}>
                                <VisitStamp waiting={visitsOnSelected.every((b) => b.status === "Venter" || b.status === "VenterKunde")}/> Du har time denne dagen
                            </p>
                            <ul>
                                {visitsOnSelected.map((b) => (
                                    <li key={b.id}>
                                        <strong>{formatTime(b.startUtc)}–{formatTime(b.endUtc)}</strong>{" "}
                                        {b.serviceName} hos {b.businessName}
                                    </li>
                                ))}
                            </ul>
                            <p className={styles.muted}>Tider som krysser den er grået ut. <NextLink href="/min-besti">Se Min BESTI</NextLink></p>
                        </div>
                    )}
                </div>
            </div>
            {/* Venteliste: only alone and for customers (a business account cannot book) */}
            {!together && !chosen && !me?.isBusiness && waitlistDays.length > 0 && (
                me ? (
                    <WaitlistForm key={month} serviceId={service.id} employeeId={employeeId ?? null}
                                  days={waitlistDays} selectedDay={selected?.date}/>
                ) : (
                    <p className={styles.muted}>
                        Passer ingen av tidene?{" "}
                        <NextLink href={`/logg-inn?neste=${encodeURIComponent(currentUrl)}`}>Logg inn</NextLink>
                        {" "}og få beskjed hvis det blir ledig.
                    </p>
                )
            )}
            {tid && !chosen && (
                <p className={styles.error} role="alert">Den valgte tiden er ikke lenger ledig. Velg en annen tid.</p>
            )}

            {chosen && selected && (
                <section>
                    <h2 className={styles.sectionTitle}>{!together ? "Dine opplysninger" : persons === 2 ? "Inviter din BESTI" : "Inviter BESTI team"}</h2>
                    <p className={styles.muted}>
                        {formatDay(selected.date)} kl. {formatTime(chosen.startUtc)}
                        {/* Together: the others get other employees, so only a chosen employee is named */}
                        {chosenEmployee && (!together || employeeId) && ` hos ${chosenEmployee.name}`}
                        {together && ` · ${persons} personer${organizerJoins ? " med deg" : ", uten deg"}`}
                        {` · ${formatLength(units * service.durationMinutes)}`}
                    </p>
                    <p className={styles.muted}>{freeCancelText(business.freeCancelHours)}.</p>
                    {!me && (
                        <p className={styles.muted}>
                            Du må være logget inn for å bestille.{" "}
                            <NextLink href={`/logg-inn?neste=${encodeURIComponent(currentUrl)}`}>Logg inn</NextLink>
                            {" "}eller <NextLink href={`/registrer?neste=${encodeURIComponent(currentUrl)}`}>opprett
                            konto</NextLink>.
                        </p>
                    )}
                    {me?.isBusiness && (
                        <p className={styles.muted}>Du er logget inn som bedrift. Logg inn med en kundekonto for å
                            bestille.</p>
                    )}
                    {me && !me.isBusiness && together && (
                        <InviteForm businessSlug={business.slug} serviceId={service.id} employeeId={employeeId ?? null}
                                    startUtc={chosen.startUtc} date={selected.date} units={units} fullName={me.fullName}
                                    phone={me.phone} persons={persons} organizerJoins={organizerJoins}/>)}
                    {me && !me.isBusiness && !together && (
                        <BookingForm businessSlug={business.slug} serviceId={service.id} employeeId={chosen.employeeId}
                                     startUtc={chosen.startUtc} date={selected.date} units={units} fullName={me.fullName}
                                     email={me.email} phone={me.phone}/>)}
                </section>
            )}
        </main>
    );
}
