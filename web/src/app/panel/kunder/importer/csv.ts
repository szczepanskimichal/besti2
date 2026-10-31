// "Importer kunder": the CSV file is read in the browser, so the business can choose the columns first.
// Every system exports a bit differently: ; or , or tab, UTF-8 or the old Windows letters from Excel

export type ImportField = "" | "name" | "firstName" | "lastName" | "phone" | "email" | "note";

export const fieldLabels: Record<ImportField, string> = {
    "": "Ikke bruk",
    name: "Fullt navn",
    firstName: "Fornavn",
    lastName: "Etternavn",
    phone: "Telefon",
    email: "E-post",
    note: "Notat",
};

// Bigger lists are split in more files (the API takes 5000 customers at a time)
export const MaxFileBytes = 3 * 1024 * 1024;
export const MaxRows = 5000;

// Excel saves "CSV" in UTF-8, UTF-16 ("Unicode-tekst") or Windows-1252: æ, ø and å must survive all of them
export function decodeFile(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let text: string;
    if (bytes[0] === 0xff && bytes[1] === 0xfe) {
        text = new TextDecoder("utf-16le").decode(bytes);
    } else {
        try {
            text = new TextDecoder("utf-8", { fatal: true }).decode(bytes);
        } catch {
            text = new TextDecoder("windows-1252").decode(bytes);
        }
    }
    return text.replace(/^﻿/, "");
}

// The separator that splits the first line into the most columns
function guessDelimiter(text: string): string {
    const firstLine = text.split(/\r?\n/, 1)[0] ?? "";
    let best = ",";
    let bestCount = 0;
    for (const delimiter of [";", ",", "\t"]) {
        let count = 0;
        let quoted = false;
        for (const char of firstLine) {
            if (char === "\"") quoted = !quoted;
            else if (!quoted && char === delimiter) count++;
        }
        if (count > bestCount) {
            best = delimiter;
            bestCount = count;
        }
    }
    return best;
}

// Rows of cells; quotes, "" inside quotes and line breaks inside quotes are handled, empty rows left out
export function parseCsv(text: string): string[][] {
    const delimiter = guessDelimiter(text);
    const rows: string[][] = [];
    let row: string[] = [];
    let cell = "";
    let quoted = false;

    for (let i = 0; i < text.length; i++) {
        const char = text[i];
        if (quoted) {
            if (char === "\"" && text[i + 1] === "\"") {
                cell += "\"";
                i++;
            } else if (char === "\"") {
                quoted = false;
            } else {
                cell += char;
            }
        } else if (char === "\"" && cell.trim() === "") {
            cell = "";
            quoted = true;
        } else if (char === delimiter) {
            row.push(cell.trim());
            cell = "";
        } else if (char === "\n" || char === "\r") {
            if (char === "\r" && text[i + 1] === "\n") i++;
            row.push(cell.trim());
            rows.push(row);
            row = [];
            cell = "";
        } else {
            cell += char;
        }
    }
    row.push(cell.trim());
    rows.push(row);

    return rows.filter((r) => r.some((c) => c !== ""));
}

// A first guess from the column name (Norwegian and English); the business can always change it
export function guessField(header: string): ImportField {
    const h = header.toLowerCase().replace(/[^a-zæøå]/g, "");
    if (["fornavn", "firstname", "first", "givenname"].includes(h)) return "firstName";
    if (["etternavn", "lastname", "last", "surname", "familyname"].includes(h)) return "lastName";
    if (["navn", "name", "fulltnavn", "fullname", "kunde", "kundenavn", "client", "clientname", "customer",
        "customername"].includes(h)) return "name";
    if (/telefon|mobil|phone|tlf|mobile/.test(h)) return "phone";
    if (/epost|email|mail/.test(h)) return "email";
    if (/notat|note|merknad|kommentar|comment/.test(h)) return "note";
    return "";
}

// One field per column: a later column with the same guess is left as "Ikke bruk"
export function guessFields(headers: string[]): ImportField[] {
    const used = new Set<ImportField>();
    return headers.map((header) => {
        const field = guessField(header);
        if (field === "" || used.has(field)) return "";
        used.add(field);
        return field;
    });
}

// Does the first row look like column names (no phone number or e-mail in it)?
export function looksLikeHeader(row: string[]): boolean {
    return row.some((cell) => guessField(cell) !== "")
        && !row.some((cell) => cell.includes("@") || (cell.replace(/\D/g, "").length >= 8));
}

export type MappedRow = { line: number; name: string; phone: string; email: string; note: string };

// The cells as customers; line = the line in the file, so the API can say where a problem is
export function mapRows(rows: string[][], fields: ImportField[], hasHeader: boolean): MappedRow[] {
    const cell = (row: string[], field: ImportField) => {
        const index = fields.indexOf(field);
        return index < 0 ? "" : (row[index] ?? "").trim();
    };
    return rows.slice(hasHeader ? 1 : 0).map((row, i) => ({
        line: i + (hasHeader ? 2 : 1),
        name: cell(row, "name") || [cell(row, "firstName"), cell(row, "lastName")].filter(Boolean).join(" "),
        phone: cell(row, "phone"),
        email: cell(row, "email"),
        note: cell(row, "note"),
    }));
}
