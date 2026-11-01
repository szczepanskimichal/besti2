"use client";
import { useActionState } from "react";
import { deleteGalleryImageAction } from "./actions";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import styles from "./page.module.css";

// "Slett" (the blue cancel pill) asks first (a photo cannot be brought back); an error shows right under it
export default function DeleteImageForm({ imageId }: { imageId: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(deleteGalleryImageAction, {});

    return (
        <details className={styles.more}>
            <summary className={styles.pillDanger}>Slett</summary>
            <form action={formAction} className={styles.sheet}>
                <p className={styles.sheetText}>Er du sikker? Bildet kan ikke hentes tilbake.</p>
                {state.error && (
                    <p className={styles.error} role="alert">
                        {state.error}
                    </p>
                )}
                <input type="hidden" name="imageId" value={imageId} />
                <SubmitButton className={styles.dangerButton} pendingText="Sletter…">
                    Ja, slett
                </SubmitButton>
            </form>
        </details>
    );
}