"use client";
import CrystalIcon from "@/components/CrystalIcon/CrystalIcon";
import PersonsIcon from "@/components/PersonsIcon/PersonsIcon";
import { useActionState } from "react";
import NextLink from "next/link";
import { createInviteAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

type InviteFormProps = {
    businessSlug: string;
    serviceId: string;
    employeeId: string | null; // the inviter's own choice; null = Hvem som helst
    startUtc: string;
    date: string;
    units: number;
    fullName: string | null;
    phone: string | null; // from "Min profil"
    persons: number; // all visits in the group, the inviter's own included when organizerJoins
    organizerJoins: boolean;
};

// The answer times the API allows (InviteRules.DecisionMinutes); 30 is the default
const ANSWER_TIMES = [
    { minutes: 15, label: "15 min" },
    { minutes: 30, label: "30 min" },
    { minutes: 60, label: "1 time" },
    { minutes: 180, label: "3 timer" },
    { minutes: 1440, label: "1 døgn" },
];

// BESTI sammen: the inviter's own details, who pays and how long the others have to answer.
// On an error (e.g. the time was just taken) everything typed stays
export default function InviteForm({ businessSlug, serviceId, employeeId, startUtc, date, units, fullName, phone, persons, organizerJoins }: InviteFormProps) {
    const group = persons > 2 || !organizerJoins;
    const [state, formAction] = useActionState<FormState, FormData>(createInviteAction, {});
    const value = (field: string, fallback = "") => state.values?.[field] ?? fallback;

    return (
        <form action={formAction} className={styles.form}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            <p className={styles.muted}>Inviterer som <strong>{fullName}</strong></p>
            <input type="hidden" name="businessSlug" value={businessSlug}/>
            <input type="hidden" name="serviceId" value={serviceId}/>
            <input type="hidden" name="employeeId" value={employeeId ?? ""}/>
            <input type="hidden" name="startUtc" value={startUtc}/>
            <input type="hidden" name="date" value={date}/>
            <input type="hidden" name="units" value={units}/>
            <input type="hidden" name="persons" value={persons}/>
            <input type="hidden" name="organizerJoins" value={organizerJoins ? "1" : "0"}/>

            <fieldset className={styles.choices}>
                <legend>Hvem betaler?</legend>
                <label className={styles.choice}>
                    <input type="radio" name="payMode" value="HverSelv"
                           defaultChecked={value("payMode", "HverSelv") === "HverSelv"}/>
                    <span>Hver betaler selv</span>
                </label>
                <label className={styles.choice}>
                    <input type="radio" name="payMode" value="JegSpanderer"
                           defaultChecked={value("payMode") === "JegSpanderer"}/>
                    <span>{group ? "Jeg betaler for alle" : "Jeg spanderer"}{" "}<CrystalIcon name="gave"/></span>
                </label>
            </fieldset>
            <p className={styles.muted}>Dere betaler hos bedriften, som vanlig.</p>

            <fieldset className={styles.choices}>
                <legend>{group ? "Svarfrist for de andre" : "Svarfrist for vennen din"}</legend>
                {ANSWER_TIMES.map(({ minutes, label }) => (
                    <label key={minutes} className={styles.choice}>
                        <input type="radio" name="decisionMinutes" value={minutes}
                               defaultChecked={value("decisionMinutes", "30") === String(minutes)}/>
                        <span>{label}</span>
                    </label>
                ))}
            </fieldset>
            <p className={styles.muted}>
                {group
                    ? "Hver plass bestilles i det noen sier ja. Når fristen er ute eller gruppa er full, får bedriften det endelige antallet. Kort frist = større sjanse for at tidene er ledige."
                    : "Tiden holdes ikke av før vennen din svarer. Kort frist = større sjanse for at den er ledig."}
                {" "}Fristen er aldri senere enn én time før timen.
            </p>

            <label className={styles.field}>
                Din telefon
                <input name="phone" type="tel" inputMode="tel" required maxLength={20}
                       autoComplete="tel" pattern="\+?[0-9 ]{8,20}" placeholder="912 34 567"
                       title="Bare tall, mellomrom og +, minst 8 sifre"
                       defaultValue={value("phone", phone ?? "")}/>
            </label>
            <label className={styles.field}>
                Melding til bedriften (valgfritt)
                <textarea name="note" maxLength={500} rows={3}
                          placeholder={group ? "F.eks. «Firmatur, vi kommer fra kontoret»" : "F.eks. «Vi vil gjerne sitte ved siden av hverandre»"}
                          defaultValue={value("note")}/>
            </label>
            <p className={styles.muted}>
                Ved å sende godtar du <NextLink href="/vilkar">vilkårene</NextLink> og
                bekrefter at du har lest <NextLink href="/personvern">personvernerklæringen</NextLink>.
            </p>
            <SubmitButton className={styles.primaryButton} pendingText="Lager invitasjon…">
                Lag invitasjon <PersonsIcon persons={2}/>
            </SubmitButton>
        </form>
    );
}
