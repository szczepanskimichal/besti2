"use client";

// Opens the browser's print window (there the owner can also choose "Lagre som PDF")
export default function PrintButton({ className }: { className?: string }) {
    return (
        <button type="button" onClick={() => window.print()} className={className}>
            Skriv ut eller lagre som PDF
        </button>
    );
}
