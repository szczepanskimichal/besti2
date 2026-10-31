"use client";
import { useState } from "react";
import { declineInviteAction } from "../actions";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import QuickMessage from "./QuickMessage";
import styles from "./page.module.css";

// "Nei takk": first a friendly line to the inviter ("Sorry, jeg rekker det ikke"), then the answer is sent
export default function DeclineForm({ token, inviter }: { token: string; inviter: string }) {
    const [open, setOpen] = useState(false);
    if (!open) {
        return (
            <button type="button" className={styles.cancelButton} onClick={() => setOpen(true)}>
                Nei takk, ikke denne gangen
            </button>
        );
    }
    return (
        <form action={declineInviteAction} className={styles.declineBox}>
            <input type="hidden" name="token" value={token}/>
            <QuickMessage to={inviter} suggestions={[
                { text: "Sorry, jeg rekker det ikke", icon: "trist" },
                { text: "Jeg kan dessverre ikke denne gangen", icon: "trist" },
                { text: "Takk for at du spurte!", icon: "hjerte" },
                { text: "Neste gang er jeg med!", icon: "tommel" },
            ]}/>
            <SubmitButton className={styles.cancelButton} pendingText="Sender…">
                Send og si nei takk
            </SubmitButton>
        </form>
    );
}
