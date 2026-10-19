"use client";
import { useState, type ChangeEvent } from "react";
import BusinessLogo from "@/components/BusinessLogo/BusinessLogo";
import { uploadLogoAction } from "./actions";
import styles from "../tjenester/page.module.css";
// Files up to this size are sent as they are (keeps transparent PNG logos)
const SmallEnoughBytes = 900 * 1024;
// Longest side after shrinking; the API makes the final logo 512 px
const MaxSize = 1600;
// Shrinks a large photo in the browser, so it fits the upload limits
async function shrinkImage(file: File): Promise<File> {
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
    name: string;
    category: string;
    logoUrl: string | null;
};
export default function LogoUploadForm({ name, category, logoUrl }: Props) {
    const [preparing, setPreparing] = useState(false);
    async function handleChange(event: ChangeEvent<HTMLInputElement>) {
        const input = event.currentTarget;
        const file = input.files?.[0];
        if (!file || file.size <= SmallEnoughBytes) {
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
        }
    }

    return (
        <form action={uploadLogoAction} className={styles.form}>
            <BusinessLogo name={name} category={category} logoUrl={logoUrl} size="large" />
            <label className={styles.field}>
                Logo (PNG, JPG eller WebP, maks 10 MB)
                <input type="file" name="logo" accept="image/png,image/jpeg,image/webp" required onChange={handleChange} />
            </label>
            <button type="submit" className={styles.primaryButton} disabled={preparing}>
                {preparing ? "Forbereder bildet …" : "Last opp logo"}
            </button>
        </form>
    );
}