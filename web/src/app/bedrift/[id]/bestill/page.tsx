import NextLink from "next/link";
import {notFound} from "next/navigation";
import {getAvailability, getBusiness, getMe} from "@/lib/api";
import {getToken} from "@/lib/auth";
import {formatDate, formatPrice, formatTime, isIsoDate, todayInOslo} from "@/lib/format";
import BookingForm from "./BookingForm";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; dato?: string; tid?: string; }>;
};

export default async function BookingPage({params, searchParams}: Props) {
    const {id} = await params;
    const {tjeneste, ansatt, dato, tid} = await searchParams;

    const business = await getBusiness(id);
    const service = business?.services.find((s) => s.id === tjeneste);

    if (!business || !service) {
        notFound();
    }


    // Only values that still make sense (old "Bestill igjen" links, edited or cut-off addresses)
    const employeeId = business.employees.some((e) => e.id === ansatt) ? ansatt! : business.employees[0]?.id;
    const date = isIsoDate(dato) ? dato : todayInOslo();
    // Free times and who is booking (null = not logged in), both at the same time
    const token = await getToken();
    const [slots, me] = await Promise.all([
        employeeId ? getAvailability(service.id, employeeId, date) : null,
        token ? getMe(token) : null,
    ]);
    // The chosen time must still be free; otherwise the customer picks a new one
    const chosenTime = tid && slots?.includes(tid) ? tid : undefined;
    const currentUrl = `/bedrift/${business.id}/bestill?tjeneste=${service.id}&ansatt=${employeeId}&dato=${date}&tid=${encodeURIComponent(chosenTime ?? "")}`;
    
    return (
        <main className={styles.main}>
            <NextLink href={`/bedrift/${business.id}`} className={styles.back}>← Tilbake til {business.name}</NextLink>

            <h1 className={styles.title}>Bestill {service.name}</h1>
            <p className={styles.muted}>
                {service.durationMinutes} min · {formatPrice(service.priceNok, service.priceType)}
            </p>

            <form method="get" className={styles.filters}>
                <input type="hidden" name="tjeneste" value={service.id}/>

                <label className={styles.field}>
                    Ansatt
                    <select name="ansatt" defaultValue={employeeId}>
                        {business.employees.map((employee) => (
                            <option key={employee.id} value={employee.id}>
                                {employee.name} {employee.lastName}
                            </option>
                        ))}
                    </select>
                </label>

                <label className={styles.field}>
                    Dato
                    <input type="date" name="dato" defaultValue={date} min={todayInOslo()}/>
                </label>

                <button type="submit" className={styles.secondaryButton}>Vis ledige tider</button>
            </form>

            <h2 className={styles.sectionTitle}>Ledige tider</h2>

            {!slots || slots.length === 0 ? (
                <p className={styles.muted}>Ingen ledige tider denne dagen. Prøv en annen dato.</p>
            ) : (
                <ul className={styles.slots}>
                    {slots.map((slot) => (
                        <li key={slot}>
                            <NextLink
                                href={`/bedrift/${business.id}/bestill?tjeneste=${service.id}&ansatt=${employeeId}&dato=${date}&tid=${encodeURIComponent(slot)}`}
                                className={slot === chosenTime ? `${styles.slot} ${styles.slotSelected}` : styles.slot}
                            >
                                aria-current={slot === chosenTime ? "true" : undefined}
                                {formatTime(slot)}
                            </NextLink>
                        </li>
                    ))}
                </ul>
            )}
            {tid && !chosenTime && (
                <p className={styles.error} role="alert">Den valgte tiden er ikke lenger ledig. Velg en annen tid.</p>
            )}
            {chosenTime && (
                <section>
                    <h2 className={styles.sectionTitle}>Dine opplysninger</h2>
                    <p className={styles.muted}>
                        {formatDate(chosenTime)} kl. {formatTime(chosenTime)}
                    </p>
                    {!me && (
                        <p className={styles.muted}>
                            Du må være logget inn for å bestille.{" "}
                            <NextLink href={`/logg-inn?neste=${encodeURIComponent(currentUrl)}`}>Logg inn</NextLink>
                            {" "}eller <NextLink href={`/registrer?neste=${encodeURIComponent(currentUrl)}`}>opprett konto</NextLink>.
                        </p>
                    )}
                    {me?.isBusiness && (
                        <p className={styles.muted}>Du er logget inn som bedrift. Logg inn med en kundekonto for å
                            bestille.</p>
                    )}
                    {me && !me.isBusiness && (
                        <BookingForm businessId={business.id} serviceId={service.id} employeeId={employeeId}
                                     startUtc={chosenTime} date={date} fullName={me.fullName} email={me.email}/>
                    )}
                        </section>
                        )}
                </main>
            );
}
