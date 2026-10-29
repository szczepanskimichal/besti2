"use client";
import { useFormStatus } from "react-dom";

type SubmitButtonProps = {
    children: React.ReactNode;
    className?: string;
    pendingText?: string;
    disabled?: boolean; // e.g. while a photo is being prepared
};
// Submit button that is disabled and shows a text while the form's Server Action runs
export default function SubmitButton({ children, className, pendingText = "Sender…", disabled = false }: SubmitButtonProps) {
    const { pending } = useFormStatus();
    return (
        <button type="submit" className={className} disabled={pending || disabled}>
            {pending ? pendingText : children}
        </button>
    );
}