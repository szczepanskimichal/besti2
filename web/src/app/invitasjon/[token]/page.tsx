import type { CSSProperties } from "react";
import type { Metadata } from "next";
import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getInvite, getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatLength, formatPrice, formatTime } from "@/lib/format";
import { SITE_URL } from "@/lib/site";
import type { Invite } from "@/lib/types";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { withdrawInviteAction } from "../actions";
import AcceptForm from "./AcceptForm";
import DeclineForm from "./DeclineForm";
import InviteChat from "./InviteChat";
import Countdown from "./Countdown";
import LiveStatus from "./LiveStatus";
import ShareInvite from "./ShareInvite";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ token: string }>;
    searchParams: Promise<{ svar?: string }>;
};

// The card WhatsApp, Messenger and Snap show for the link (the picture is opengraph-image.tsx).
// Not in Google (layout), but chat apps may read it: the inviter shares it on purpose
export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
    const { token } = await params;
    const invite = /^[A-Za-z0-9_-]{20,100}$/.test(token) ? await getInvite(token).catch(() => null) : null;
    if (!invite) {
        return { title: "Invitasjon" };
    }
    const title = `${invite.inviterFirstName} inviterer deg 👯`;
    const places = invite.persons === 2 && invite.organizerJoins ? "" : ` ${invite.placesLeft} av ${invite.persons} plasser ledige.`;
    const description = `${invite.serviceName} hos ${invite.businessName}, ${formatDate(invite.startUtc)} kl. ${formatTime(invite.startUtc)}. Bli med?${places}`;
    return { title, description, openGraph: { title, description, type: "website", siteName: "BESTI" } };
}

// BESTI sammen: one page for everyone with the link. The inviter shares it and watches the answers come in
// live; the others see what, where, when and who is already in, and say "Jeg blir med!" or "Nei takk"
export default async function InvitePage({ params, searchParams }: Props) {
    const { token } = await params;
    const { svar } = await searchParams;
    if (!/^[A-Za-z0-9_-]{20,100}$/.test(token)) {
        notFound();
    }
    const auth = await getToken();
    const [invite, me] = await Promise.all([getInvite(token, auth), auth ? getMe(auth) : null]);
    if (!invite) {
        notFound();
    }
    const here = `/invitasjon/${token}`;
    // Two people (the inviter and one friend) keep the words for two; anything else is a group
    const pair = invite.persons === 2 && invite.organizerJoins;
    const open = invite.open && (invite.status === "Venter" || invite.status === "Akseptert");

    return (
        <main className={styles.main}>
            <div className={styles.card}>
                {open && invite.isInviter && (
                    <>
                        <LiveStatus/>
                        <Badge waiting/>
                        <h1 className={styles.title}>
                            {invite.joined > (invite.organizerJoins ? 1 : 0) ? `${invite.joined} av ${invite.persons} er med` : "Invitasjonen er klar!"}
                        </h1>
                        <p className={styles.muted}>
                            {pair
                                ? "Så snart din BESTI trykker «Jeg blir med!», er dere begge booket, og du ser det her med en gang."
                                : "Send den gjerne i gruppechatten. Hver som trykker «Jeg blir med!» får sin time med en gang, og du ser det her live."}
                        </p>
                        <FriendMessages invite={invite}/>
                        {!invite.stillFree && <TakenWarning invite={invite}/>}
                        <h2 className={styles.sendTitle}>{pair ? "Send invitasjonen til din BESTI" : "Send invitasjonen"}</h2>
                        {/* "Du venter på svar" shows up once the link is sent (any of the share buttons) */}
                        <ShareInvite url={`${SITE_URL}${here}`} text={shareText(invite)}>
                            {!pair && <Seats invite={invite}/>}
                            <Deadline label="Svarfrist" invite={invite}
                                      who={pair ? "Du venter på svar fra din BESTI" : "Du venter på svar fra gjengen"}/>
                        </ShareInvite>
                        <When invite={invite}/>
                        <Details invite={invite}/>
                        {invite.chat && <InviteChat token={token} lines={invite.chat} myName={invite.inviterFirstName} pair={pair} poll={false}/>}
                        {invite.status === "Venter" && (
                            <form action={withdrawInviteAction}>
                                <input type="hidden" name="inviteId" value={invite.id}/>
                                <input type="hidden" name="token" value={token}/>
                                <SubmitButton className={styles.cancelButton} pendingText="Trekker tilbake…">
                                    Trekk tilbake invitasjonen
                                </SubmitButton>
                            </form>
                        )}
                    </>
                )}

                {/* "Nei takk" in a group: the others can still join, but this visitor just gets a thank you */}
                {open && !invite.isInviter && !invite.isMember && svar === "nei" && (
                    <Closed title="Takk for svaret 💚"
                            text={`Vi har gitt beskjed til ${invite.inviterFirstName}. Ombestemt deg? Åpne lenken på nytt.`}
                            invite={invite}/>
                )}

                {open && !invite.isInviter && !invite.isMember && svar !== "nei" && (
                    <>
                        <Badge waiting/>
                        <h1 className={styles.title}>
                            {pair ? `${invite.inviterFirstName} inviterer deg til BESTI-tid sammen!` : `${invite.inviterFirstName} inviterer til ${invite.serviceName}!`}
                        </h1>
                        <p className={styles.muted}>
                            {pair
                                ? `${invite.serviceName} samtidig, hver med sin egen ansatt.`
                                : `Alle samtidig, hver med sin egen ansatt. ${invite.placesLeft === 1 ? "Én plass" : `${invite.placesLeft} plasser`} igjen, førstemann til mølla!`}
                        </p>
                        {invite.payMode === "JegSpanderer" && (
                            <p className={styles.gift}>
                                <CrystalIcon name="gave"/> {invite.inviterFirstName} spanderer
                            </p>
                        )}
                        {svar === "feil" && (
                            <p className={styles.error} role="alert">Svaret kunne ikke lagres. Prøv igjen.</p>
                        )}
                        <When invite={invite}/>
                        <Details invite={invite}/>
                        {!pair && <Seats invite={invite}/>}
                        {invite.stillFree ? (
                            <>
                                <Deadline label="Svarfrist" invite={invite} who={`${invite.inviterFirstName} venter på svaret ditt`}/>
                                {!me && (
                                    <div className={styles.actions}>
                                        <NextLink href={`/logg-inn?neste=${encodeURIComponent(here)}`}
                                                  className={styles.primaryButton}>
                                            Logg inn og bli med
                                        </NextLink>
                                        <NextLink href={`/registrer?neste=${encodeURIComponent(here)}`}
                                                  className={styles.secondaryButton}>
                                            Ny på BESTI? Opprett konto
                                        </NextLink>
                                    </div>
                                )}
                                {me?.isBusiness && (
                                    <p className={styles.muted}>
                                        Du er logget inn som bedrift. Logg inn med en kundekonto for å bli med.
                                    </p>
                                )}
                                {me && !me.isBusiness && <AcceptForm token={token} fullName={me.fullName} phone={me.phone} inviter={invite.inviterFirstName}/>}
                            </>
                        ) : (
                            <TakenWarning invite={invite}/>
                        )}
                        <DeclineForm token={token} inviter={invite.inviterFirstName}/>
                    </>
                )}

                {/* Booked: a member (also while others can still join), or the inviter once the group is closed */}
                {invite.status === "Akseptert" && (invite.isInviter ? !open : invite.isMember) && (
                    <>
                        {open && <LiveStatus/>}
                        <Badge/>
                        <h1 className={styles.title}>
                            {pair ? "Dere er booket!" : open ? "Du er med!" : "Gruppa er klar!"}
                        </h1>
                        <p className={styles.muted}>
                            {pair
                                ? invite.isInviter
                                    ? `${invite.names.find((n) => n !== invite.inviterFirstName) ?? "Vennen din"} ble med!`
                                    : `Du og ${invite.inviterFirstName} har time sammen.`
                                : `${invite.joined} ${invite.joined === 1 ? "person" : "personer"} ${open ? "er med så langt" : "kommer"}: ${listNames(invite.names)}.`}{" "}
                            {invite.payMode === "JegSpanderer" &&
                                (invite.isInviter ? (pair ? "Du spanderer 🎁. " : "Du betaler for alle 🎁. ")
                                    : `${invite.inviterFirstName} spanderer 🎁. `)}
                            {invite.isMember ? "Timen din ligger i Min BESTI." : "Bedriften har fått beskjed om hvor mange som kommer."}
                        </p>
                        {invite.isInviter && <FriendMessages invite={invite}/>}
                        <Seats invite={invite}/>
                        {open && !pair && (
                            <>
                                <Deadline label="Flere kan bli med i" invite={invite}/>
                                <ShareInvite url={`${SITE_URL}${here}`} text={shareText(invite)}/>
                            </>
                        )}
                        <When invite={invite} done/>
                        <Details invite={invite}/>
                        {invite.chat && (
                            <InviteChat token={token} lines={invite.chat} pair={pair} poll={!open}
                                        myName={invite.isInviter ? invite.inviterFirstName : me?.fullName?.split(" ")[0] ?? "Meg"}/>
                        )}
                        <NextLink href="/min-besti" className={styles.primaryButton}>Se timen i Min BESTI</NextLink>
                    </>
                )}

                {invite.status === "Akseptert" && !open && !invite.isInviter && !invite.isMember && (
                    <Closed title={invite.placesLeft === 0 ? (pair ? "Invitasjonen er allerede besvart" : "Gruppa er full") : "Svarfristen er ute"}
                            text={pair
                                ? `${invite.inviterFirstName} har allerede fått med seg en venn.`
                                : `${invite.joined} har allerede sagt ja, og ${invite.inviterFirstName} har fått beskjed.`}
                            invite={invite}/>
                )}

                {invite.status === "Avslatt" && (
                    <Closed title={svar === "nei" ? "Takk for svaret 💚" : "Ikke denne gangen"}
                            text={svar === "nei"
                                ? `Vi har gitt beskjed til ${invite.inviterFirstName}. Ingenting ble bestilt.`
                                : invite.isInviter
                                    ? "Vennen din kan dessverre ikke. Ingenting ble bestilt."
                                    : "Invitasjonen er besvart."}
                            invite={invite}/>
                )}
                {invite.status === "Avslatt" && invite.isInviter && <FriendMessages invite={invite}/>}

                {invite.status === "Utlopt" && (
                    <Closed title="Svarfristen er ute"
                            text="Ingen svar i tide, så ingenting ble bestilt. Tiden kan fortsatt være ledig."
                            invite={invite}/>
                )}

                {invite.status === "Trukket" && (
                    <Closed title="Invitasjonen er trukket tilbake"
                            text={invite.isInviter ? "Ingenting ble bestilt." : `${invite.inviterFirstName} har trukket invitasjonen tilbake.`}
                            invite={invite}/>
                )}
            </div>
        </main>
    );
}

// "Bli med meg på Herreklipp hos Klipp & Stil fredag 30. oktober kl. 10:00? 👯"
function shareText(invite: Invite): string {
    const treat = invite.payMode === "JegSpanderer" ? " Jeg spanderer! 🎁" : "";
    const when = `${formatDate(invite.startUtc)} kl. ${formatTime(invite.startUtc)}`;
    if (invite.persons === 2 && invite.organizerJoins) {
        return `Bli med meg på ${invite.serviceName} hos ${invite.businessName} ${when}? 👯${treat}`;
    }
    const places = invite.placesLeft === 1 ? "1 plass" : `${invite.placesLeft} plasser`;
    return `${invite.serviceName} hos ${invite.businessName} ${when}, hvem blir med? 👯 ${places} igjen.${treat}`;
}

// "Kari, Ola og Per"
function listNames(names: string[]): string {
    return names.length <= 1 ? names.join("") : `${names.slice(0, -1).join(", ")} og ${names[names.length - 1]}`;
}

// The places as crystal circles: a first letter (mint) for everyone booked, a lilac "?" for each place still waiting
function Seats({ invite }: { invite: Invite }) {
    const free = Array.from({ length: invite.open ? invite.placesLeft : 0 });
    return (
        <div className={styles.seats}>
            <ul className={styles.seatList}>
                {invite.names.map((name, i) => (
                    <li key={`${name}-${i}`} className={styles.seat} title={name}>
                        <span className={styles.seatCircle} aria-hidden="true">{name.slice(0, 1).toUpperCase()}</span>
                        <span className={styles.seatName}>{name}</span>
                    </li>
                ))}
                {free.map((_, i) => (
                    <li key={`free-${i}`} className={`${styles.seat} ${styles.seatFree}`}>
                        <span className={styles.seatCircle} aria-hidden="true">?</span>
                        <span className={styles.seatName}>{invite.persons > 2 ? "Ledig" : invite.isInviter ? "BESTI?" : "Deg?"}</span>
                    </li>
                ))}
            </ul>
            {invite.persons > 2 && (
                <p className={styles.seatsCount}>
                    <strong>{invite.joined} av {invite.persons}</strong> plasser tatt
                    {!invite.organizerJoins && ` · ${invite.inviterFirstName} inviterer, uten egen time`}
                </p>
            )}
        </div>
    );
}

// Day and time big in a glowing mint frame, like "Ny tid"
function When({ invite, done = false }: { invite: Invite; done?: boolean }) {
    return (
        <div className={done ? `${styles.when} ${styles.whenDone}` : styles.when}>
            <span className={styles.label}>Når</span>
            <span className={styles.whenDay}>{formatDate(invite.startUtc)}</span>
            <span className={styles.whenTime}>kl. {formatTime(invite.startUtc)}</span>
        </div>
    );
}

// "Ada venter på svaret ditt / Svarfrist 23 t 17 min" in a lilac pill (lilac = waiting, the same as the Venter stamp)
function Deadline({ label, invite, who }: { label: string; invite: Invite; who?: string }) {
    return (
        <div className={styles.deadline}>
            {who && <p className={styles.deadlineWho}>{who}</p>}
            <p className={styles.deadlineRow}>
                <span className={styles.deadlineDot} aria-hidden="true"/>
                <span className={styles.deadlineLabel}>{label}</span>
                <Countdown expiresUtc={invite.expiresUtc} className={styles.clock}/>
            </p>
        </div>
    );
}

function Details({ invite }: { invite: Invite }) {
    return (
        <dl className={styles.details}>
            <dt>Bedrift</dt>
            <dd>
                <NextLink href={`/${invite.businessSlug}`}>{invite.businessName}</NextLink>
            </dd>
            <dt>Tjeneste</dt>
            <dd>{invite.serviceName} · {formatLength(invite.visitMinutes)}</dd>
            <dt>Adresse</dt>
            <dd>{invite.businessAddress}</dd>
            <dt>Pris</dt>
            <dd>
                {formatPrice(invite.priceNok, invite.priceType)} per person
                <br/>
                <span className={styles.muted}>
                    {invite.payMode === "JegSpanderer"
                        ? `${invite.isInviter ? "Du" : invite.inviterFirstName} spanderer${invite.persons > 2 ? " for alle" : ""}`
                        : "Hver betaler selv, hos bedriften"}
                </span>
            </dd>
        </dl>
    );
}

// What the friends wrote with their answer, as small lilac-mint speech bubbles (only the inviter sees them)
function FriendMessages({ invite }: { invite: Invite }) {
    if (!invite.friendMessages?.length) return null;
    return (
        <ul className={styles.friendMessages}>
            {invite.friendMessages.map((m, i) => (
                <li key={i} className={m.joined ? styles.bubble : `${styles.bubble} ${styles.bubbleNo}`}>
                    <span className={styles.bubbleName}>{m.name}{m.joined ? " ble med" : " kan ikke"}</span>
                    <span className={styles.bubbleText}>
                        «{m.text}»
                        {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                        {m.icon && <img src={`/ikoner/${m.icon}.svg`} alt="" aria-hidden="true" className={styles.chipIcon}/>}
                    </span>
                </li>
            ))}
        </ul>
    );
}

// Someone else booked the place in the meantime
function TakenWarning({ invite }: { invite: Invite }) {
    return (
        <div className={styles.warning} role="status">
            <p>Noen andre har nettopp bestilt hos {invite.businessName} på denne tiden, så det er ikke lenger en ledig plass.</p>
            <NextLink href={`/${invite.businessSlug}`}>Finn en ny tid hos {invite.businessName} →</NextLink>
        </div>
    );
}

function Closed({ title, text, invite }: { title: string; text: string; invite: Invite }) {
    return (
        <>
            <h1 className={styles.title}>{title}</h1>
            <p className={styles.muted}>{text}</p>
            {invite.isInviter && <FriendMessages invite={invite}/>}
            <Details invite={invite}/>
            <NextLink href={`/${invite.businessSlug}`} className={styles.secondaryButton}>
                Se {invite.businessName}
            </NextLink>
        </>
    );
}

// Twelve sparks fly out when the two crystals meet (same as the booking confirmation)
const SPARK_COLORS = ["#ffffff", "#bff8e8", "#72d6bd", "#8fc8ff", "#c9bfff"];
const SPARKS = Array.from({ length: 12 }, (_, k) => SPARK_COLORS[k % 5]);

// Two crystals, one for each besti. Waiting: a lilac halo and they lean towards each other.
// Booked: they meet in a mint halo, a check mark draws itself and sparks fly out
function Badge({ waiting = false }: { waiting?: boolean }) {
    return (
        <div className={waiting ? `${styles.badge} ${styles.badgeWaiting}` : styles.badge} aria-hidden="true">
            <span className={styles.halo}/>
            {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
            <img src="/ikoner/krystall.svg" alt="" className={styles.gem}/>
            {/* eslint-disable-next-line @next/next/no-img-element -- static SVG with the glow baked in */}
            <img src="/ikoner/krystall.svg" alt="" className={`${styles.gem} ${styles.gemRight}`}/>
            {!waiting && (
                <>
                    <svg className={styles.check} viewBox="-10 -10 120 120">
                        <path d="M37 51 l9 9 l18 -20" pathLength={1}/>
                    </svg>
                    <span className={styles.sparks}>
                        {SPARKS.map((color, k) => (
                            <i key={k} style={{ "--a": `${k * 30 + 10}deg`, "--d": `${80 + (k % 3) * 14}px`, color } as CSSProperties}>
                                {k % 2 ? "✦" : "✧"}
                            </i>
                        ))}
                    </span>
                </>
            )}
        </div>
    );
}
