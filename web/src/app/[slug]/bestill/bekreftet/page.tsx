import type { CSSProperties } from "react";
import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getBusinessBySlug } from "@/lib/api";
import { formatDate, formatTime } from "@/lib/format";
import ShareButton from "@/components/ShareButton/ShareButton";
import RebookReminderForm from "./RebookReminderForm";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ slug: string }>;
    searchParams: Promise<{ tjeneste?: string; ansatt?: string; tid?: string }>;
};

// Twelve sparks fly out around the crystal in Besti colours
const SPARK_COLORS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];
const SPARKS = Array.from({ length: 12 }, (_, k) => SPARK_COLORS[k % 5]);

export default async function BookingConfirmedPage({ params, searchParams }: Props) {
    const { slug } = await params;
    const { tjeneste, ansatt, tid } = await searchParams;

    const business = await getBusinessBySlug(slug);
    const service = business?.services.find((s) => s.id === tjeneste);
    const employee = business?.employees.find((e) => e.id === ansatt);

    // A cut-off or edited link (e.g. tid=abc) shows "not found" instead of crashing
    if (!business || !service || !employee || !tid || Number.isNaN(Date.parse(tid))) {
        notFound();
    }

    return (
        <main className={styles.main}>
            <div className={styles.card}>
                {/* Ada's crystal: pops in with a burst and sparks, the check draws itself, then it glows softly */}
                <div className={styles.badge} aria-hidden="true">
                    <span className={styles.halo}/>
                    <span className={styles.burst}/>
                    {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
                    <img src="/ikoner/krystall.svg" alt="" className={styles.gem}/>
                    <svg className={styles.check} viewBox="-10 -10 120 120">
                        <path d="M37 51 l9 9 l18 -20" pathLength={1}/>
                    </svg>
                    <span className={styles.sparks}>
                        {SPARKS.map((color, k) => (
                            <i key={k} style={{ "--a": `${k * 30 + 10}deg`, "--d": `${70 + (k % 3) * 14}px`, color } as CSSProperties}>
                                {k % 2 ? "✦" : "✧"}
                            </i>
                        ))}
                    </span>
                    <span className={styles.glint} style={{ left: "74%", top: "6%" }}/>
                    <span className={styles.glint} style={{ left: "6%", top: "62%", animationDelay: "2.9s" }}/>
                </div>
                <h1 className={styles.title}>Vips! Så var det bestilt.</h1>
                <p className={styles.muted}>
                    {/* With "Bekreft automatisk" the visit is already confirmed */}
                    Klar for kvalitetstid med din besti? 💖 {business.autoConfirm
                    ? "Timen er bekreftet"
                    : `${business.name} bekrefter timen så snart som mulig`}, og du finner den under Min Besti.
                </p>

                <dl className={styles.details}>
                    <dt>Tjeneste</dt>
                    <dd>{service.name}</dd>
                    <dt>Hos</dt>
                    <dd>{employee.name} {employee.lastName}</dd>
                    <dt>Tid</dt>
                    <dd>{formatDate(tid)} kl. {formatTime(tid)}</dd>
                    <dt>Adresse</dt>
                    <dd>{business.address}, {business.postalCode} {business.city}</dd>
                </dl>

                <RebookReminderForm serviceId={service.id} employeeId={employee.id}/>

                {/* Friends are the best marketing: one tap sends the business page in Messenger or SMS */}
                <div className={styles.share}>
                    <p className={styles.muted}>Kjenner du noen som også trenger en time?</p>
                    <ShareButton path={`/${business.slug}`} title={business.name} label="Del med en venn"
                                 text={`Jeg har bestilt time hos ${business.name} på BESTI. Bli med?`}
                                 className={styles.shareButton}/>
                </div>

                <NextLink href="/" className={styles.back}>← Tilbake til forsiden</NextLink>
            </div>
        </main>
    );
}
