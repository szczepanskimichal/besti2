"use client";
import { useActionState, useState, type ChangeEvent } from "react";
import { MaxSendBytes, shrinkImage } from "../profil/LogoUploadForm";
import { uploadGalleryImageAction } from "./actions";
import type { FormState } from "@/lib/formState";
import styles from "./page.module.css";

// Photos up to this size are sent as they are; bigger ones are shrunk in the browser first
const SmallEnoughBytes = 900 * 1024;

export default function GalleryUploadForm() {
    const [preparing, setPreparing] = useState(false);
    const [tooBig, setTooBig] = useState(false);
    const [fileName, setFileName] = useState("");
    // isPending: the upload is running ("Laster opp …"), so the button cannot be clicked twice
    const [state, formAction, isPending] = useActionState<FormState, FormData>(uploadGalleryImageAction, {});

    async function handleChange(event: ChangeEvent<HTMLInputElement>) {
        const input = event.currentTarget;
        const file = input.files?.[0];
        setFileName(file?.name ?? "");
        if (!file || file.size <= SmallEnoughBytes) {
            setTooBig(false);
            return;
        }
        setPreparing(true);
        try {
            // Put the smaller file back into the input, so the form sends it instead
            const smaller = await shrinkImage(file);
            const transfer = new DataTransfer();
            transfer.items.add(smaller);
            input.files = transfer.files;
        } catch {
            // Browser could not read the file; send the original and let the API answer
        } finally {
            setPreparing(false);
            // Still too big (the browser could not shrink it): say so instead of an error page
            setTooBig((input.files?.[0]?.size ?? 0) > MaxSendBytes);
        }
    }

    return (
        <form action={formAction} className={styles.upload}>
            {state.error && (
                <p className={styles.error} role="alert">
                    {state.error}
                </p>
            )}
            {/* One row in the glowing field: choose a photo, then upload it */}
            <div className={styles.drop}>
                <div className={styles.uploadRow}>
                    <label className={styles.choose}>
                        Velg bilde
                        <input type="file" name="image" accept="image/png,image/jpeg,image/webp" required onChange={handleChange}
                               className={styles.fileInput}/>
                    </label>
                    <button type="submit" className={styles.saveButton} disabled={preparing || isPending || tooBig}>
                        {preparing ? "Forbereder …" : isPending ? "Laster opp …" : "Last opp bilde"}
                    </button>
                </div>
                <span className={fileName ? styles.fileName : styles.dropHint}>
                    {fileName || "PNG, JPG eller WebP, maks 10 MB"}
                </span>
            </div>
            {tooBig && (
                <p className={styles.error} role="alert">
                    Bildet er for stort. Velg et bilde under 3,5 MB.
                </p>
            )}
        </form>
    );
}