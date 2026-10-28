import Link from "next/link";
import type { BusinessDetails } from "@/lib/types";
import { hideFirstStepsAction } from "./actions";
import styles from "./FirstSteps.module.css";

// Sign-up gives Mon–Fri 09–17 and the business itself as the only employee
function hasDefaultHours(business: BusinessDetails) {
    return business.openingHours.length === 5
        && business.openingHours.every((h) => h.dayOfWeek >= 1 && h.dayOfWeek <= 5
            && h.opensAt.startsWith("09:00") && h.closesAt.startsWith("17:00"));
}

function hasOnlyDefaultEmployee(business: BusinessDetails) {
    return business.employees.length === 1
        && business.employees[0].name === business.name && !business.employees[0].lastName;
}

// "Kom i gang": what a new business should fill in before customers come, worked out from its own data.
// Gone when everything is done, or when the owner presses "Skjul"
export default function FirstSteps({ business }: { business: BusinessDetails }) {
    const steps = [
        {
            done: business.services.length > 0,
            href: "/panel/tjenester",
            title: "Legg til tjenester",
            text: "Med pris og varighet. Uten tjenester kan ingen bestille.",
        },
        {
            done: !hasDefaultHours(business),
            href: "/panel/apningstider",
            title: "Sjekk åpningstidene",
            text: "Nå står det mandag–fredag 09–17. Endre dem hvis det ikke stemmer.",
        },
        {
            done: !hasOnlyDefaultEmployee(business),
            href: "/panel/ansatte",
            title: "Legg til ansatte",
            text: "Deg selv og de andre med navn, så kunden kan velge hvem.",
        },
        {
            done: business.logoUrl !== null && !!business.description,
            href: "/panel/profil",
            title: "Logo og beskrivelse",
            text: "Logoen vises i søket, beskrivelsen øverst på siden din.",
        },
        {
            done: business.gallery.length > 0,
            href: "/panel/galleri",
            title: "Bilder",
            text: "Noen bilder av lokalet eller arbeidet ditt gir mer tillit.",
        },
    ];
    const doneCount = steps.filter((s) => s.done).length;
    if (doneCount === steps.length) {
        return null;
    }

    return (
        <section className={styles.box} aria-labelledby="kom-i-gang">
            <div className={styles.head}>
                <h2 id="kom-i-gang" className={styles.title}>Kom i gang</h2>
                <span className={styles.count}>{doneCount} av {steps.length} klare</span>
            </div>
            <ol className={styles.steps}>
                {steps.map((step) => (
                    <li key={step.href} className={step.done ? styles.done : undefined}>
                        <span className={styles.mark} aria-hidden="true">{step.done ? "✓" : ""}</span>
                        <div>
                            <Link href={step.href} className={styles.stepTitle}>
                                {step.title}
                                <span className={styles.srOnly}>{step.done ? " (ferdig)" : " (ikke ferdig)"}</span>
                            </Link>
                            {!step.done && <p className={styles.text}>{step.text}</p>}
                        </div>
                    </li>
                ))}
                <li>
                    <span className={styles.mark} aria-hidden="true">→</span>
                    <div>
                        <Link href="/panel/del" className={styles.stepTitle}>Del lenken og heng opp plakaten</Link>
                        <p className={styles.text}>Kundene dine finner deg via QR-koden, Instagram og Google.</p>
                    </div>
                </li>
            </ol>
            <form action={hideFirstStepsAction}>
                <button type="submit" className={styles.hide}>Skjul listen</button>
            </form>
        </section>
    );
}
