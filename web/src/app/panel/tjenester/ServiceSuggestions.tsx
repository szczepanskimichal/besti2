"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import type { ServiceTemplate } from "@/lib/types";
import { formatPrice } from "@/lib/format";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { addSuggestionsAction } from "./actions";
import styles from "./tjenester.module.css";

type Props = {
    templates: ServiceTemplate[];
    // Open at once for a business without services; folded when it already has some
    open: boolean;
};

// "Forslag for din bransje": tick what you offer, change price and length, add them all with one press
export default function ServiceSuggestions({ templates, open }: Props) {
    const [state, formAction] = useActionState<FormState, FormData>(addSuggestionsAction, {});
    // After an error the form shows what the owner chose and typed (React empties it after the action)
    const value = (field: string, fallback: string) => state.values?.[field] ?? fallback;

    return (
        <details className={styles.suggestions} open={open || !!state.error}>
            <summary className={styles.addTitle}>
                <span className={styles.star} aria-hidden="true">
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon */}
                    <img src="/ikoner/lyspare.svg" alt=""/>
                </span>
                Forslag for din bransje ({templates.length})
            </summary>
            <p className={styles.muted}>
                Kryss av det dere tilbyr, og endre pris og tid så de passer. Alt kan endres senere.
            </p>
            <form action={formAction} className={styles.suggestionForm}>
                {state.error && (
                    <p className={styles.error} role="alert">
                        {state.error}
                    </p>
                )}
                <input type="hidden" name="antall" value={templates.length}/>
                <ul className={styles.suggestionList}>
                    {templates.map((template, i) => (
                        <li key={template.name} className={styles.suggestion}>
                            <input type="hidden" name={`navn-${i}`} value={template.name}/>
                            <label className={styles.suggestionName}>
                                <input type="checkbox" name={`valgt-${i}`}
                                       defaultChecked={value(`valgt-${i}`, "") === "on"}/>
                                <span>
                                    {template.name}
                                    {template.description && (
                                        <span className={styles.suggestionHint}>{template.description}</span>
                                    )}
                                </span>
                            </label>
                            <div className={styles.suggestionFields}>
                                <label>
                                    <span className={styles.visuallyHidden}>Varighet for {template.name}</span>
                                    <input name={`tid-${i}`} type="number" required min={5} max={480} step={5}
                                           defaultValue={value(`tid-${i}`, String(template.durationMinutes))}/>
                                    <span aria-hidden="true">{template.priceType === "PerTime" ? "min/time" : "min"}</span>
                                </label>
                                {template.priceType === "EtterAvtale" ? (
                                    <span className={styles.muted}>{formatPrice(null, "EtterAvtale")}</span>
                                ) : (
                                    <label>
                                        <span className={styles.visuallyHidden}>Pris for {template.name}</span>
                                        <input name={`pris-${i}`} type="number" required min={0} step={1}
                                               defaultValue={value(`pris-${i}`, String(template.priceNok ?? 0))}/>
                                        <span aria-hidden="true">{template.priceType === "PerTime" ? "kr/time" : "kr"}</span>
                                    </label>
                                )}
                            </div>
                        </li>
                    ))}
                </ul>
                <SubmitButton className={styles.primaryButton} pendingText="Legger til…">
                    Legg til valgte
                </SubmitButton>
            </form>
        </details>
    );
}
