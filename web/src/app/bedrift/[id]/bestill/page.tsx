import NextLink from "next/link";
import {notFound} from "next/navigation";
import {getAvailability, getBusiness, getMe} from "@/lib/api";
import {getToken} from "@/lib/auth";
import {formatDate, formatPrice, formatTime, todayInOslo} from "@/lib/format";
import {createBookingAction} from "./actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; dato?: string; tid?: string; feil?: string }>;
};

export default async function BookingPage({params, searchParams}: Props) {
    const {id} = await params;
    const {tjeneste, ansatt, dato, tid, feil} = await searchParams;

    const business = await getBusiness(id);
    const service = business?.services.find((s) => s.id === tjeneste);

    if (!business || !service) {
        notFound();
    }

    const employeeId = ansatt ?? business.employees[0]?.id;
    const date = dato ?? todayInOslo();
    const slots = employeeId ? await getAvailability(service.id, employeeId, date) : null;

    // Who is booking: null = not logged in
    const token = await getToken();
    const me = token ? await getMe(token) : null;
    const currentUrl = `/bedrift/${business.id}/bestill?tjeneste=${service.id}&ansatt=${employeeId}&dato=${date}&tid=${encodeURIComponent(tid ?? "")}`;
    
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
                                className={slot === tid ? `${styles.slot} ${styles.slotSelected}` : styles.slot}
                            >
                                {formatTime(slot)}
                            </NextLink>
                        </li>
                    ))}
                </ul>
            )}
            {tid && (
                <section>
                    <h2 className={styles.sectionTitle}>Dine opplysninger</h2>
                    <p className={styles.muted}>
                        Valgt tid: <strong>{formatDate(tid)} kl. {formatTime(tid)}</strong>
                    </p>
                    {feil && <p className={styles.error} role="alert">{feil}</p>}
                    {!me && (
                        <p className={styles.muted}>
                            Du må være logget inn for å bestille.{" "}
                            <NextLink href={`/logg-inn?neste=${encodeURIComponent(currentUrl)}`}>Logg inn</NextLink>
                            {" "}eller <NextLink href="/registrer">opprett konto</NextLink>.
                        </p>
                    )}
                    {me?.isBusiness && (
                        <p className={styles.muted}>Du er logget inn som bedrift. Logg inn med en kundekonto for å
                            bestille.</p>
                    )}
                    {me && !me.isBusiness && (
                        <form action={createBookingAction} className={styles.form}>
                            <p className={styles.muted}>Bestiller som <strong>{me.fullName}</strong> ({me.email})</p>
                            <input type="hidden" name="businessId" value={business.id}/>
                            <input type="hidden" name="serviceId" value={service.id}/>
                            <input type="hidden" name="employeeId" value={employeeId}/>
                            <input type="hidden" name="startUtc" value={tid}/>
                            <input type="hidden" name="date" value={date}/>
                            <label className={styles.field}>
                                Telefon
                                <input name="customerPhone" type="tel" required maxLength={20} autoComplete="tel"/>
                            </label>
                            <label className={styles.field}>
                                Melding til bedriften (valgfritt)
                                <textarea name="note" maxLength={500} rows={3}
                                          placeholder="F.eks. «Jeg har langt hår»"/>
                            </label>
                            <label className={styles.checkbox}>
                                <input type="checkbox" name="marketingConsent"/>
                                Ja, jeg vil motta nyhetsbrev og tilbud på e-post
                            </label>
                            <p className={styles.muted}>
                                Ved å bestille godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
                            </p>
                            <SubmitButton className={styles.primaryButton} pendingText="Bestiller…">Bekreft bestilling</SubmitButton>                        </form>
                    )}
                        </section>
                        )}
                </main>
            );
}
