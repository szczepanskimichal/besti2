"use client";
import Link from "next/link";
import { useState, useTransition, type ChangeEvent } from "react";
import type { CustomerImportResult } from "@/lib/types";
import panel from "../../page.module.css";
import styles from "../page.module.css";
import { importCustomersAction } from "./actions";
import {
    MaxFileBytes, MaxRows, decodeFile, fieldLabels, guessFields, looksLikeHeader, mapRows, parseCsv,
    type ImportField,
} from "./csv";

type Parsed = { fileName: string; rows: string[][]; fields: ImportField[]; hasHeader: boolean };

const fieldOptions = Object.keys(fieldLabels) as ImportField[];

// 1 choose the file, 2 choose what each column is, 3 BESTI checks the list, 4 import
export default function ImportCustomers() {
    const [parsed, setParsed] = useState<Parsed | null>(null);
    const [check, setCheck] = useState<CustomerImportResult | null>(null);
    const [done, setDone] = useState<CustomerImportResult | null>(null);
    const [confirmed, setConfirmed] = useState(false);
    const [error, setError] = useState("");
    const [isPending, startTransition] = useTransition();

    async function handleFile(event: ChangeEvent<HTMLInputElement>) {
        const file = event.currentTarget.files?.[0];
        setError("");
        setCheck(null);
        if (!file) return;
        if (/\.(xlsx|xls|numbers)$/i.test(file.name)) {
            setError("Dette er en Excel- eller Numbers-fil. Åpne den og velg «Lagre som» → «CSV» først.");
            return;
        }
        if (file.size > MaxFileBytes) {
            setError("Filen er for stor (maks 3 MB). Del den i flere filer.");
            return;
        }
        const rows = parseCsv(decodeFile(await file.arrayBuffer()));
        if (rows.length === 0) {
            setError("Fant ingen kunder i filen.");
            return;
        }
        const width = Math.max(...rows.map((r) => r.length));
        const hasHeader = looksLikeHeader(rows[0]);
        const fields = hasHeader
            ? guessFields(rows[0])
            : Array<ImportField>(width).fill("");
        setParsed({ fileName: file.name, rows, fields: [...fields, ...Array<ImportField>(width - fields.length).fill("")], hasHeader });
    }

    function setField(column: number, field: ImportField) {
        if (!parsed) return;
        setCheck(null);
        // One column per field: the old column with this field goes back to "Ikke bruk"
        const fields = parsed.fields.map((f, i) => (i === column ? field : f === field && field !== "" ? "" : f));
        setParsed({ ...parsed, fields });
    }

    const mapped = parsed ? mapRows(parsed.rows, parsed.fields, parsed.hasHeader) : [];
    const hasName = parsed?.fields.some((f) => f === "name" || f === "firstName") ?? false;
    const hasPhone = parsed?.fields.includes("phone") ?? false;

    function send(save: boolean) {
        setError("");
        if (mapped.length > MaxRows) {
            setError(`Filen har ${mapped.length} kunder. Maks ${MaxRows} om gangen: del den i flere filer.`);
            return;
        }
        startTransition(async () => {
            const state = await importCustomersAction(mapped, save, confirmed);
            if (state.error || !state.result) {
                setError(state.error ?? "Noe gikk galt. Prøv igjen.");
            } else if (save) {
                setDone(state.result);
            } else {
                setCheck(state.result);
            }
        });
    }

    function startOver() {
        setParsed(null);
        setCheck(null);
        setDone(null);
        setConfirmed(false);
        setError("");
    }

    if (done) {
        return (
            <section className={panel.card} aria-live="polite">
                <h2 className={styles.cardTitle}>Vips! {done.added} {done.added === 1 ? "kunde" : "kunder"} importert.</h2>
                <p className={styles.facts}>
                    {done.existing > 0 && <span>{done.existing} fantes fra før</span>}
                    {done.notesAdded > 0 && <span>{done.notesAdded} {done.notesAdded === 1 ? "notat" : "notater"} lagt til</span>}
                    {done.skipped > 0 && <span>{done.skipped} hoppet over</span>}
                </p>
                <div className={styles.importActions}>
                    <Link href="/panel/kunder" className={styles.saveButton}>Se kundene</Link>
                    <button type="button" className={panel.secondaryButton} onClick={startOver}>Importer en fil til</button>
                </div>
            </section>
        );
    }

    return (
        <>
            {error && <p className={panel.error} role="alert">{error}</p>}

            <section className={panel.card} aria-labelledby="fil">
                <h2 id="fil" className={styles.cardTitle}>1. Velg filen</h2>
                <label className={styles.fileField}>
                    <span>CSV-fil med kundene (maks {MaxRows} kunder, 3 MB)</span>
                    <input type="file" accept=".csv,.txt,text/csv,text/plain" onChange={handleFile}/>
                </label>
                {parsed && (
                    <p className={panel.muted}>
                        {parsed.fileName}: {mapped.length} {mapped.length === 1 ? "rad" : "rader"}.
                    </p>
                )}
            </section>

            {parsed && (
                <section className={panel.card} aria-labelledby="kolonner">
                    <h2 id="kolonner" className={styles.cardTitle}>2. Hva er i hver kolonne?</h2>
                    <label className={styles.checkLine}>
                        <input type="checkbox" checked={parsed.hasHeader}
                               onChange={(e) => { setCheck(null); setParsed({ ...parsed, hasHeader: e.target.checked }); }}/>
                        Første rad er kolonnenavn
                    </label>
                    <ul className={styles.columns}>
                        {parsed.fields.map((field, column) => {
                            const samples = parsed.rows.slice(parsed.hasHeader ? 1 : 0, parsed.hasHeader ? 3 : 2)
                                .map((r) => r[column] ?? "").filter(Boolean);
                            return (
                                <li key={column} className={styles.column}>
                                    <label className={styles.columnLabel} htmlFor={`kolonne-${column}`}>
                                        {parsed.hasHeader && parsed.rows[0][column] ? parsed.rows[0][column] : `Kolonne ${column + 1}`}
                                    </label>
                                    <span className={styles.columnSample}>{samples.join(", ") || "(tom)"}</span>
                                    <select id={`kolonne-${column}`} value={field} className={styles.select}
                                            onChange={(e) => setField(column, e.target.value as ImportField)}>
                                        {fieldOptions.map((option) => (
                                            <option key={option} value={option}>{fieldLabels[option]}</option>
                                        ))}
                                    </select>
                                </li>
                            );
                        })}
                    </ul>
                    {(!hasName || !hasPhone) ? (
                        <p className={styles.noteHint}>Velg minst en kolonne for navn og en for telefon.</p>
                    ) : (
                        <>
                            <h3 className={styles.previewTitle}>Slik blir de første kundene</h3>
                            <ul className={styles.preview}>
                                {mapped.slice(0, 3).map((row) => (
                                    <li key={row.line}>
                                        <strong>{row.name || "(uten navn)"}</strong> · {row.phone || "(uten telefon)"}
                                        {row.email && <> · {row.email}</>}
                                        {row.note && <span className={styles.columnSample}>📝 {row.note}</span>}
                                    </li>
                                ))}
                            </ul>
                            <button type="button" className={styles.saveButton} disabled={isPending}
                                    onClick={() => send(false)}>
                                {isPending && !check ? "Sjekker …" : "Sjekk listen"}
                            </button>
                        </>
                    )}
                </section>
            )}

            {parsed && check && (
                <section className={panel.card} aria-labelledby="sjekk" aria-live="polite">
                    <h2 id="sjekk" className={styles.cardTitle}>3. Klar til import</h2>
                    <p className={styles.facts}>
                        <span className={styles.next}>{check.added} {check.added === 1 ? "ny kunde" : "nye kunder"}</span>
                        {check.existing > 0 && <span>{check.existing} finnes allerede</span>}
                        {check.duplicates > 0 && <span>{check.duplicates} dobbelt i filen</span>}
                        {check.skipped > 0 && <span className={styles.noShow}>{check.skipped} hoppes over</span>}
                        {check.notesAdded > 0 && <span>{check.notesAdded} {check.notesAdded === 1 ? "notat" : "notater"}</span>}
                    </p>
                    {check.problems.length > 0 && (
                        <ul className={styles.problems}>
                            {check.problems.map((problem) => (
                                <li key={`${problem.line}-${problem.message}`}>Rad {problem.line}: {problem.message}</li>
                            ))}
                        </ul>
                    )}
                    {check.added === 0 && check.notesAdded === 0 ? (
                        <p className={panel.muted}>Ingen nye kunder i filen. Alle finnes allerede under Kunder.</p>
                    ) : (
                        <>
                            {/* The business is the data controller of its customer list, BESTI only stores it */}
                            <label className={styles.checkLine}>
                                <input type="checkbox" checked={confirmed} onChange={(e) => setConfirmed(e.target.checked)}/>
                                Kundene er våre egne kunder, og vi har lov til å flytte listen til BESTI. Vi sender
                                dem ikke reklame uten at de har sagt ja.
                            </label>
                            <button type="button" className={styles.saveButton} disabled={isPending || !confirmed}
                                    onClick={() => send(true)}>
                                {isPending ? "Importerer …" : `Importer ${check.added} ${check.added === 1 ? "kunde" : "kunder"}`}
                            </button>
                        </>
                    )}
                </section>
            )}
        </>
    );
}
