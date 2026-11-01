"use client";
import { useActionState, useState, type ChangeEvent } from "react";
import { uploadLogoAction } from "./actions";
import type { FormState } from "@/lib/formState";
import styles from "./profil.module.css";
// Files up to this size are sent as they are (keeps transparent PNG logos)
const SmallEnoughBytes = 900 * 1024;
// Bigger than this can not be sent at all (the server accepts 4 MB per form); shown as a message instead
export const MaxSendBytes = 3.5 * 1024 * 1024;
// Longest side after shrinking; the API makes the final logo 512 px
const MaxSize = 1600;
// Shrinks a large photo in the browser, so it fits the upload limits
export async function shrinkImage(file: File): Promise<File> {
    const bitmap = await createImageBitmap(file); // also turns phone photos upright
    const scale = Math.min(1, MaxSize / Math.max(bitmap.width, bitmap.height));
    const canvas = document.createElement("canvas");
    canvas.width = Math.round(bitmap.width * scale);
    canvas.height = Math.round(bitmap.height * scale);
    const context = canvas.getContext("2d");
    if (!context) {
        return file;
    }
// JPEG has no transparency, so use a white background like the logo frame
    context.fillStyle = "#fff";
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    bitmap.close();
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.85));
    return blob ? new File([blob], "logo.jpg", { type: "image/jpeg" }) : file;
}
type Props = {
    // A logo is there already: the pill says "Bytt logo" instead of "Last opp logo"
    hasLogo: boolean;
};
// The logo itself is drawn by the page, next to the name; this is only the pill and the upload sheet
export default function LogoUploadForm({ hasLogo }: Props) {
    const [preparing, setPreparing] = useState(false);
    const [tooBig, setTooBig] = useState(false);
    // isPending: the upload is running ("Laster opp …"), so the button cannot be clicked twice
    const [state, formAction, isPending] = useActionState<FormState, FormData>(uploadLogoAction, {});
    async function handleChange(event: ChangeEvent<HTMLInputElement>) {
        const input = event.currentTarget;
        const file = input.files?.[0];
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
        // Changing the logo is not done every day, so the file field sits behind a small pill
        <details className={styles.more} open={!!state.error || tooBig}>
            <summary className={styles.pill}>{hasLogo ? "Bytt logo" : "Last opp logo"}</summary>
            <form action={formAction} className={styles.sheet}>
                {state.error && (
                    <p className={styles.error} role="alert">
                        {state.error}
                    </p>
                )}
                <label className={styles.field}>
                    <span>PNG, JPG eller WebP, maks 10 MB</span>
                    <input type="file" name="logo" accept="image/png,image/jpeg,image/webp" required
                           onChange={handleChange}/>
                </label>
                {tooBig && (
                    <p className={styles.error} role="alert">
                        Bildet er for stort. Velg et bilde under 3,5 MB.
                    </p>
                )}
                <button type="submit" className={styles.saveButton} disabled={preparing || isPending || tooBig}>
                    {preparing ? "Forbereder bildet …" : isPending ? "Laster opp …" : "Last opp logo"}
                </button>
            </form>
        </details>
    );
}
