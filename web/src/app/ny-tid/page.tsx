import type { Metadata } from "next";
import Link from "next/link";
import { getNewTime } from "@/lib/api";
import { formatDate, formatTime, sameDay } from "@/lib/format";
import { privatePage } from "@/lib/site";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { answerNewTimeAction } from "./actions";
// Same card, crystal and buttons as the booking confirmation, so the two pages feel like one
import confirm from "../[slug]/bestill/bekreftet/page.module.css";
import styles from "./page.module.css";

export const metadata: Metadata = {
    title: "Ny tid",
    robots: privatePage,
};

type NewTimePageProps = {
    searchParams: Promise<{ token?: string; svar?: string }>;
};

// Ada's crystal with a clock inside at the new time: the minute hand ticks and a ring runs down,
// like a countdown until the customer answers
function ClockBadge() {
    return (
        <div className={confirm.badge} aria-hidden="true">
            <span className={confirm.halo}/>
            {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
            <img src="/ikoner/krystall.svg" alt="" className={confirm.gem}/>
            <svg className={styles.clock} viewBox="0 0 100 100">
                {/* Ring that runs down again and again: the time waits for the answer */}
                <circle className={styles.ring} cx="50" cy="50" r="56" pathLength={1}/>
                <line className={styles.minute} x1="50" y1="50" x2="50" y2="68"/>
                <line className={styles.hour} x1="50" y1="50" x2="50" y2="37"/>
                <circle className={styles.pin} cx="50" cy="50" r="3.5"/>
            </svg>
        </div>
    );
}

// The same crystal with a check mark, when the answer is saved
function CheckBadge() {
    return (
        <div className={confirm.badge} aria-hidden="true">
            <span className={confirm.halo}/>
            <span className={confirm.burst}/>
            {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
            <img src="/ikoner/krystall.svg" alt="" className={confirm.gem}/>
            <svg className={confirm.check} viewBox="-10 -10 120 120">
                <path d="M37 51 l9 9 l18 -20" pathLength={1}/>
            </svg>
        </div>
    );
}

// "Passer den nye tiden?": the page behind the link in the e-mail. The link only shows the question;
// the answer is a button, so a mail program that opens links on its own cannot answer for the customer
export default async function NewTimePage({ searchParams }: NewTimePageProps) {
    const { token, svar } = await searchParams;

    if (svar === "godtatt") {
        return (
            <main className={confirm.main}>
                <div className={`${confirm.card} ${styles.card}`}>
                    <CheckBadge/>
                    <h1 className={confirm.title}>Da er det avtalt!</h1>
                    <p className={confirm.muted} role="status">Bedriften har fått beskjed, og den nye tiden står i Min BESTI.</p>
                    <Link href="/min-besti" className={confirm.minBesti}>Se timen i Min BESTI</Link>
                    <Link href="/" className={confirm.back}>Tilbake til forsiden</Link>
                </div>
            </main>
        );
    }

    if (svar === "avslatt") {
        return (
            <main className={confirm.main}>
                <div className={`${confirm.card} ${styles.card}`}>
                    <h1 className={confirm.title}>Timen er avlyst</h1>
                    <p className={confirm.muted} role="status">Bedriften har fått beskjed. Finn en ny tid når det passer deg.</p>
                    <Link href="/" className={confirm.minBesti}>Finn en ny time</Link>
                    <Link href="/min-besti" className={confirm.back}>Se timene dine i Min BESTI</Link>
                </div>
            </main>
        );
    }

    const newTime = svar || !token ? null : await getNewTime(token);
    if (!newTime) {
        return (
            <main className={confirm.main}>
                <div className={`${confirm.card} ${styles.card}`}>
                    <h1 className={confirm.title}>Lenken er ikke gyldig lenger</h1>
                    <p className={confirm.muted} role="status">
                        {svar === "feil"
                            ? "Svaret kunne ikke lagres. Kanskje du allerede har svart, eller timen har startet."
                            : "Kanskje du allerede har svart, eller bedriften har endret timen igjen."}
                    </p>
                    <Link href="/min-besti" className={confirm.minBesti}>Se timene dine i Min BESTI</Link>
                    <Link href="/" className={confirm.back}>Tilbake til forsiden</Link>
                </div>
            </main>
        );
    }

    return (
        <main className={confirm.main}>
            <div className={`${confirm.card} ${styles.card}`}>
                <ClockBadge/>
                <h1 className={confirm.title}>Passer den nye tiden?</h1>
                <p className={styles.info}>
                    {newTime.businessName} har endret tiden for timen din. Den gjelder først når du har godtatt den.
                </p>
                {/* Same lavender stamp as "Venter på bekreftelse" in Min BESTI; it is gone once the customer answers */}
                <span className={styles.stamp} role="img" aria-label="Status: venter på svaret ditt">
                    <span aria-hidden="true">VENTER PÅ<small>SVARET DITT</small></span>
                </span>

                {/* Old time crossed out, the new one big and bright */}
                <div className={styles.swap}>
                    {newTime.previousStartUtc && (
                        <p className={styles.old}>
                            <span className={styles.label}>Før</span>
                            <s>{sameDay(newTime.previousStartUtc, newTime.startUtc) ? "" : `${formatDate(newTime.previousStartUtc)} `}kl. {formatTime(newTime.previousStartUtc)}</s>
                        </p>
                    )}
                    <p className={styles.new}>
                        <span className={styles.label}>Ny tid</span>
                        <span className={styles.newDate}>{formatDate(newTime.startUtc)}</span>
                        <span className={styles.newTime}>kl. {formatTime(newTime.startUtc)}</span>
                    </p>
                </div>

                <dl className={confirm.details}>
                    <dt>Bedrift</dt>
                    <dd>{newTime.businessName}</dd>
                    <dt>Tjeneste</dt>
                    <dd>{newTime.serviceName}</dd>
                    <dt>Hos</dt>
                    <dd>{newTime.employeeName}</dd>
                </dl>

                <form action={answerNewTimeAction}>
                    <input type="hidden" name="token" value={token}/>
                    <input type="hidden" name="svar" value="ja"/>
                    <SubmitButton className={`${confirm.minBesti} ${styles.yes}`} pendingText="Lagrer…">Ja, det passer</SubmitButton>
                </form>
                <form action={answerNewTimeAction}>
                    <input type="hidden" name="token" value={token}/>
                    <input type="hidden" name="svar" value="nei"/>
                    <SubmitButton className={styles.no} pendingText="Lagrer…">Passer ikke, avlys timen</SubmitButton>
                </form>
                <p className={styles.note}>Bedriften får beskjed med en gang du svarer.</p>
            </div>
        </main>
    );
}
