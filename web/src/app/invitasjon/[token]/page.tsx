import type { Metadata } from "next";
import NextLink from "next/link";
import { notFound } from "next/navigation";
import { getInvite, getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { formatDate, formatLength, formatPrice, formatTime } from "@/lib/format";
import { SITE_URL } from "@/lib/site";
import type { Invite } from "@/lib/types";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { declineInviteAction, withdrawInviteAction } from "../actions";
import AcceptForm from "./AcceptForm";
import Countdown from "./Countdown";
import LiveStatus from "./LiveStatus";
import ShareInvite from "./ShareInvite";
import styles from "./page.module.css";

type Props = {
    params: Promise<{ token: string }>;
    searchParams: Promise<{ ny?: string; svar?: string }>;
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
    const { ny, svar } = await searchParams;
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
                            {invite.joined > 0 ? `${invite.joined} av ${invite.persons} er med` : ny ? "Invitasjonen er klar!" : "Venter på svar"}
                        </h1>
                        <p className={styles.muted}>
                            {pair
                                ? "Send lenken til din besti. Så snart hen trykker «Jeg blir med!», er dere begge booket, og du ser det her med en gang."
                                : "Send lenken til alle du vil ha med, for eksempel i gruppechatten. Hver som trykker «Jeg blir med!» får sin time med en gang, og du ser det her live."}
                        </p>
                        {!pair && <Seats invite={invite}/>}
                        {!invite.stillFree && <TakenWarning invite={invite}/>}
                        <p className={styles.timer}>
                            Svarfrist <Countdown expiresUtc={invite.expiresUtc} className={styles.clock}/>
                        </p>
                        <ShareInvite url={`${SITE_URL}${here}`} text={shareText(invite)}/>
                        <Details invite={invite}/>
                        {invite.status === "Venter" && (
                            <form action={withdrawInviteAction}>
                                <input type="hidden" name="inviteId" value={invite.id}/>
                                <input type="hidden" name="token" value={token}/>
                                <SubmitButton className={styles.textButton} pendingText="Trekker tilbake…">
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
                            {pair ? `${invite.inviterFirstName} inviterer deg!` : `${invite.inviterFirstName} inviterer til ${invite.serviceName}!`}
                        </h1>
                        <p className={styles.muted}>
                            {pair
                                ? `Bli med på ${invite.serviceName} samtidig, hver med sin egen ansatt.`
                                : `Alle samtidig, hver med sin egen ansatt. ${invite.placesLeft === 1 ? "Én plass" : `${invite.placesLeft} plasser`} igjen, førstemann til mølla!`}
                        </p>
                        {!pair && <Seats invite={invite}/>}
                        {svar === "feil" && (
                            <p className={styles.error} role="alert">Svaret kunne ikke lagres. Prøv igjen.</p>
                        )}
                        <Details invite={invite}/>
                        {invite.stillFree ? (
                            <>
                                <p className={styles.timer}>
                                    Svar innen <Countdown expiresUtc={invite.expiresUtc} className={styles.clock}/>
                                </p>
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
                                {me && !me.isBusiness && <AcceptForm token={token} fullName={me.fullName} phone={me.phone}/>}
                            </>
                        ) : (
                            <TakenWarning invite={invite}/>
                        )}
                        <form action={declineInviteAction}>
                            <input type="hidden" name="token" value={token}/>
                            <SubmitButton className={styles.textButton} pendingText="Sender…">
                                Nei takk, ikke denne gangen
                            </SubmitButton>
                        </form>
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
                        {!pair && <Seats invite={invite}/>}
                        {open && !pair && (
                            <>
                                <p className={styles.timer}>
                                    Flere kan bli med i <Countdown expiresUtc={invite.expiresUtc} className={styles.clock}/>
                                </p>
                                <ShareInvite url={`${SITE_URL}${here}`} text={shareText(invite)}/>
                            </>
                        )}
                        <Details invite={invite}/>
                        <NextLink href="/min-besti" className={styles.primaryButton}>Til Min BESTI</NextLink>
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

// The places as circles: a first letter for everyone booked, an empty circle for each free place
function Seats({ invite }: { invite: Invite }) {
    const free = Array.from({ length: invite.placesLeft });
    return (
        <div className={styles.seats}>
            <p className={styles.seatsCount}>
                <strong>{invite.joined} av {invite.persons}</strong> plasser tatt
                {!invite.organizerJoins && ` · ${invite.inviterFirstName} inviterer, uten egen time`}
            </p>
            <ul className={styles.seatList}>
                {invite.names.map((name, i) => (
                    <li key={`${name}-${i}`} className={styles.seat} title={name}>
                        <span className={styles.seatCircle} aria-hidden="true">{name.slice(0, 1).toUpperCase()}</span>
                        <span className={styles.seatName}>{name}</span>
                    </li>
                ))}
                {invite.open && free.map((_, i) => (
                    <li key={`free-${i}`} className={`${styles.seat} ${styles.seatFree}`}>
                        <span className={styles.seatCircle} aria-hidden="true">+</span>
                        <span className={styles.seatName}>Ledig</span>
                    </li>
                ))}
            </ul>
        </div>
    );
}

function Details({ invite }: { invite: Invite }) {
    return (
        <dl className={styles.details}>
            <dt>Hva</dt>
            <dd>{invite.serviceName} · {formatLength(invite.visitMinutes)}</dd>
            <dt>Når</dt>
            <dd>{formatDate(invite.startUtc)} kl. {formatTime(invite.startUtc)}</dd>
            <dt>Hvor</dt>
            <dd>
                <NextLink href={`/${invite.businessSlug}`}>{invite.businessName}</NextLink>
                <br/>
                <span className={styles.muted}>{invite.businessAddress}</span>
            </dd>
            <dt>Pris</dt>
            <dd>
                {formatPrice(invite.priceNok, invite.priceType)} per person
                <br/>
                <span className={styles.muted}>
                    {invite.payMode === "JegSpanderer"
                        ? `${invite.isInviter ? "Du" : invite.inviterFirstName} spanderer${invite.persons > 2 ? " for alle" : ""} 🎁`
                        : "Hver betaler selv, hos bedriften"}
                </span>
            </dd>
        </dl>
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
            <Details invite={invite}/>
            <NextLink href={`/${invite.businessSlug}`} className={styles.secondaryButton}>
                Se {invite.businessName}
            </NextLink>
        </>
    );
}

// Two aurora circles side by side (two besties); a check mark draws itself when they are booked
function Badge({ waiting = false }: { waiting?: boolean }) {
    return (
        <div className={waiting ? `${styles.badge} ${styles.badgeWaiting}` : styles.badge} aria-hidden="true">
            <span className={styles.orb}><span className={styles.glow}/></span>
            <span className={`${styles.orb} ${styles.orbRight}`}><span className={styles.glow}/></span>
            {!waiting && (
                <svg className={styles.check} viewBox="0 0 92 92">
                    <path d="M28 47 l12 12 l24 -26"/>
                </svg>
            )}
        </div>
    );
}
