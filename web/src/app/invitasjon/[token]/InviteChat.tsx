"use client";
import { useOptimistic, useState, useTransition } from "react";
import type { ChatReaction, Invite, ReactionKind } from "@/lib/types";
import styles from "./page.module.css";
import LiveStatus from "./LiveStatus";
import { deleteChatMessageAction, reactChatMessageAction, sendChatMessageAction } from "../actions";

type Line = NonNullable<Invite["chat"]>[number];

// The crystal reactions, the same family as the stars, the bell and the heart elsewhere in BESTI
const REACTIONS: { kind: ReactionKind; label: string }[] = [
    { kind: "hjerte", label: "Hjerte" },
    { kind: "tommel", label: "Tommel opp" },
    { kind: "latter", label: "Gråter av latter" },
    { kind: "trist", label: "Trist" },
    { kind: "stjerne", label: "Stjerne" },
    { kind: "feiring", label: "Hurra" },
];
const ME = "Du";

function Icon({ kind }: { kind: ReactionKind }) {
    // eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed
    return <img src={`/ikoner/${kind}.svg`} alt="" aria-hidden="true" className={styles.reactionIcon}/>;
}

// Tap one: add it, tap it again: take it back (one of each kind per person, like in other chats)
function toggle(list: ChatReaction[] = [], kind: ReactionKind): ChatReaction[] {
    const found = list.find((r) => r.kind === kind);
    if (!found) return [...list, { kind, names: [ME] }];
    const names = found.names.includes(ME) ? found.names.filter((n) => n !== ME) : [...found.names, ME];
    return names.length ? list.map((r) => (r.kind === kind ? { ...r, names } : r)) : list.filter((r) => r.kind !== kind);
}

type Change =
    | { type: "send"; line: Line }
    | { type: "react"; id: string; kind: ReactionKind }
    | { type: "delete"; id: string };

// What the visitor just did shows at once; the server's answer then draws the page again
function apply(lines: Line[], change: Change): Line[] {
    switch (change.type) {
        case "send":
            return [...lines, change.line];
        case "react":
            return lines.map((l) => (l.id === change.id ? { ...l, reactions: toggle(l.reactions, change.kind) } : l));
        case "delete":
            return lines.filter((l) => l.id !== change.id);
    }
}

// "BESTI-chat": everyone in the invitation talks right here (who brings what, where you meet, a birthday surprise)
// instead of starting another group on social media. Only the inviter and the friends who joined see it.
// poll = ask the server for new lines every few seconds (when the page does not do it already)
export default function InviteChat({ token, lines, myName, pair, poll }:
                                       { token: string; lines: Line[]; myName: string; pair: boolean; poll: boolean }) {
    const [all, change] = useOptimistic(lines, apply);
    const [, startTransition] = useTransition();
    const [text, setText] = useState("");
    const [picking, setPicking] = useState<number | null>(null);
    const [error, setError] = useState<string | null>(null);

    function run(optimistic: Change, call: () => Promise<{ ok: boolean; message?: string }>) {
        setError(null);
        startTransition(async () => {
            change(optimistic);
            const result = await call();
            if (!result.ok) setError(result.message ?? "Noe gikk galt. Prøv igjen.");
        });
    }

    function send() {
        const t = text.trim();
        if (!t) return;
        setText("");
        run({ type: "send", line: { id: `ny-${Date.now()}`, name: myName, text: t, mine: true, canDelete: false, time: new Date().toISOString(), reactions: [] } },
            () => sendChatMessageAction(token, t));
    }

    function react(i: number, kind: ReactionKind) {
        const id = all[i].id;
        setPicking(null);
        run({ type: "react", id, kind }, () => reactChatMessageAction(token, id, kind));
    }

    function remove(id: string) {
        run({ type: "delete", id }, () => deleteChatMessageAction(token, id));
    }

    return (
        <section className={styles.chat} aria-label="BESTI-chat">
            {poll && <LiveStatus/>}
            <h2 className={styles.chatTitle}>BESTI-chat</h2>
            <p className={styles.chatHint}>
                {pair ? "Avtal detaljene her, bare dere to ser dette." : "Avtal detaljene her: hvem tar med hva, hvor dere møtes. Bare de som er med ser dette."}
            </p>
            <ul className={styles.chatLines}>
                {all.map((l, i) => (
                    <li key={l.id} className={l.mine ? `${styles.chatRow} ${styles.chatRowMine}` : styles.chatRow}>
                        {picking === i && (
                            <div className={styles.reactionPicker} role="menu" aria-label="Reager">
                                {REACTIONS.map((r) => (
                                    <button key={r.kind} type="button" role="menuitem" aria-label={r.label}
                                            className={l.reactions.find((x) => x.kind === r.kind)?.names.includes(ME) ? `${styles.reactionPick} ${styles.reactionPicked}` : styles.reactionPick}
                                            onClick={() => react(i, r.kind)}>
                                        <Icon kind={r.kind}/>
                                    </button>
                                ))}
                            </div>
                        )}
                        <div className={styles.chatBubbleWrap}>
                            <div className={l.mine ? `${styles.chatLine} ${styles.chatMine}` : styles.chatLine}
                                 onClick={() => setPicking(picking === i ? null : i)}>
                                {!l.mine && <span className={styles.chatName}>{l.name}</span>}
                                <span className={styles.chatText}>{l.text}</span>
                                <span className={styles.chatTime}>
                                    {new Date(l.time).toLocaleTimeString("nb-NO", { hour: "2-digit", minute: "2-digit", timeZone: "Europe/Oslo" })}
                                    {l.canDelete && (
                                        <button type="button" className={styles.chatDelete}
                                                onClick={(e) => { e.stopPropagation(); remove(l.id); }}>
                                            Slett
                                        </button>
                                    )}
                                </span>
                            </div>
                            <button type="button" className={styles.reactionAdd} aria-label="Reager på meldingen"
                                    aria-expanded={picking === i} onClick={() => setPicking(picking === i ? null : i)}>
                                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
                                    <circle cx="11" cy="13" r="8"/><path d="M8 15.5c1.6 1.6 4.4 1.6 6 0"/><path d="M8.5 11h.01M13.5 11h.01"/><path d="M20 2v5M17.5 4.5h5"/>
                                </svg>
                            </button>
                        </div>
                        {l.reactions.length > 0 && (
                            <div className={styles.reactions}>
                                {l.reactions.map((r) => (
                                    <button key={r.kind} type="button" title={r.names.join(", ")}
                                            className={r.names.includes(ME) ? `${styles.reaction} ${styles.reactionMine}` : styles.reaction}
                                            onClick={() => react(i, r.kind)}>
                                        <Icon kind={r.kind}/>
                                        {r.names.length > 1 && <span>{r.names.length}</span>}
                                    </button>
                                ))}
                            </div>
                        )}
                    </li>
                ))}
            </ul>
            {error && <p className={styles.error} role="alert">{error}</p>}
            <div className={styles.chatInput}>
                <textarea aria-label="Skriv en melding" rows={1} maxLength={500} placeholder="Skriv en melding…"
                          value={text} onChange={(e) => setText(e.target.value)}
                          onKeyDown={(e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); send(); } }}/>
                <button type="button" className={styles.chatSend} onClick={send} aria-label="Send">
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor" aria-hidden="true"><path d="M3 20.5 21 12 3 3.5 3 10l12 2-12 2z"/></svg>
                </button>
            </div>
        </section>
    );
}
