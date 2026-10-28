import NextLink from "next/link";
import {notFound} from "next/navigation";
import {getBusinessBySlug, getFreeDays, getMe} from "@/lib/api";
import {getToken} from "@/lib/auth";
import {formatDay, formatLength, formatPrice, formatTime, formatWeekdayShort, isIsoDate, shiftDay, todayInOslo} from "@/lib/format";
import {freeCancelText} from "@/lib/freeCancel";
import AutoSubmitSelect from "@/components/AutoSubmitSelect/AutoSubmitSelect";
import BookingForm from "./BookingForm";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ slug: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; fra?: string; dato?: string; tid?: string; antall?: string; }>;
};

export default async function BookingPage({params, searchParams}: Props) {
    const {slug} = await params;
    const {tjeneste, ansatt, fra, dato, tid, antall} = await searchParams;

    const business = await getBusinessBySlug(slug);
    const service = business?.services.find((s) => s.id === tjeneste);

    if (!business || !service) {
        notFound();
    }

    // Only values that still make sense (old "Bestill igjen" links, edited or cut-off addresses)
    const employeeId = business.employees.some((e) => e.id === ansatt) ? ansatt : undefined; // undefined = Hvem som helst
    // "Per time" services: the customer picks how many times the service length in a row, max 8 hours
    const maxUnits = service.priceType === "PerTime" ? Math.floor(480 / service.durationMinutes) : 1;
    const units = Math.min(Math.max(Math.floor(Number(antall)) || 1, 1), maxUnits);
    const today = todayInOslo();
    const chosenDay = isIsoDate(dato) && dato >= today ? dato : undefined;
    // First day of the 7-day strip
    const start = isIsoDate(fra) && fra >= today ? fra : chosenDay ?? today;
    // Free times for 7 days and who is booking (null = not logged in), both at the same time
    const token = await getToken();
    const [days, me] = await Promise.all([
        getFreeDays(service.id, employeeId, start, units),
        token ? getMe(token) : null,
    ]);
    // The chosen day, otherwise the first day with free times
    const selected = days?.find((d) => d.date === chosenDay) ?? days?.find((d) => d.slots.length > 0) ?? days?.[0];
    // The chosen time must still be free; otherwise the customer picks a new one
    const chosen = tid ? selected?.slots.find((s) => s.startUtc === tid) : undefined;
    const chosenEmployee = business.employees.find((e) => e.id === chosen?.employeeId);
    const previous = shiftDay(start, -7) < today ? today : shiftDay(start, -7);

    // Link to this page with another week, day or time (the employee choice stays)
    function bookingUrl(from: string, day?: string, time?: string) {
        const query = new URLSearchParams({tjeneste: service!.id, fra: from});
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

    const currentUrl = bookingUrl(start, selected?.date, chosen?.startUtc);

    return (
        <main className={styles.main}>
            <NextLink href={`/${business.slug}`} className={styles.back}>← Tilbake til {business.name}</NextLink>

            <h1 className={styles.title}>Bestill {service.name}</h1>
            <p className={styles.muted}>
                {formatLength(service.durationMinutes)} · {formatPrice(service.priceNok, service.priceType)}
            </p>

            <form method="get" className={styles.filters}>
                <input type="hidden" name="tjeneste" value={service.id}/>
                <input type="hidden" name="fra" value={start}/>
                {selected && <input type="hidden" name="dato" value={selected.date}/>}

                <label className={styles.field}>
                    Ansatt
                    <AutoSubmitSelect name="ansatt" defaultValue={employeeId ?? ""}>
                        <option value="">Hvem som helst</option>
                        {business.employees.map((employee) => (
                            <option key={employee.id} value={employee.id}>
                                {employee.name} {employee.lastName}
                            </option>
                        ))}
                    </AutoSubmitSelect>
                </label>

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
            <ul className={styles.days}>
                {days?.map((day) => (
                    <li key={day.date}>
                        {day.slots.length === 0 ? (
                            <span className={`${styles.day} ${styles.dayEmpty}`}>
                                <span className={styles.dayName}>{formatWeekdayShort(day.date)}</span>
                                <strong className={styles.dayNumber}>{Number(day.date.slice(8))}</strong>
                                <span className={styles.dayCount}>Ingen</span>
                            </span>
                        ) : (
                            <NextLink
                                href={bookingUrl(start, day.date)}
                                className={day.date === selected?.date ? `${styles.day} ${styles.daySelected}` : styles.day}
                                aria-current={day.date === selected?.date ? "date" : undefined}>
                                <span className={styles.dayName}>{formatWeekdayShort(day.date)}</span>
                                <strong className={styles.dayNumber}>{Number(day.date.slice(8))}</strong>
                                <span className={styles.dayCount}>{day.slots.length} ledige</span>
                            </NextLink>
                        )}
                    </li>
                ))}
            </ul>
            <nav className={styles.weekNav} aria-label="Bytt uke">
                {start > today ? (
                    <NextLink href={bookingUrl(previous)} className={styles.secondaryButton}>← Forrige</NextLink>
                ) : <span/>}
                <NextLink href={bookingUrl(shiftDay(start, 7))} className={styles.secondaryButton}>Neste 7 dager
                    →</NextLink>
            </nav>

            <h2 className={styles.sectionTitle}>Ledige tider {selected ? formatDay(selected.date) : ""}</h2>

            {!selected || selected.slots.length === 0 ? (
                <p className={styles.muted}>Ingen ledige tider disse dagene. Trykk «Neste 7 dager».</p>
            ) : (
                <ul className={styles.slots}>
                    {selected.slots.map((slot) => (
                        <li key={slot.startUtc}>
                            <NextLink
                                href={bookingUrl(start, selected.date, slot.startUtc)}
                                className={slot.startUtc === chosen?.startUtc ? `${styles.slot} ${styles.slotSelected}` : styles.slot}
                                aria-current={slot.startUtc === chosen?.startUtc ? "true" : undefined}>
                                {formatTime(slot.startUtc)}
                            </NextLink>
                        </li>
                    ))}
                </ul>
            )}
            {tid && !chosen && (
                <p className={styles.error} role="alert">Den valgte tiden er ikke lenger ledig. Velg en annen tid.</p>
            )}

            {chosen && selected && (
                <section>
                    <h2 className={styles.sectionTitle}>Dine opplysninger</h2>
                    <p className={styles.muted}>
                        {formatDay(selected.date)} kl. {formatTime(chosen.startUtc)}
                        {chosenEmployee && ` hos ${chosenEmployee.name}`}
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
                    {me && !me.isBusiness && (
                        <BookingForm businessSlug={business.slug} serviceId={service.id} employeeId={chosen.employeeId}
                                     startUtc={chosen.startUtc} date={selected.date} units={units} fullName={me.fullName}
                                     email={me.email} phone={me.phone}/>)}
                </section>
            )}
        </main>
    );
}
