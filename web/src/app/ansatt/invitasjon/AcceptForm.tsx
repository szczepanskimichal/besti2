"use client";
import { useActionState } from "react";
import type { FormState } from "@/lib/formState";
import SubmitButton from "@/components/SubmitButton/SubmitButton";
import { acceptStaffInviteAction } from "../actions";
import panelStyles from "../../panel/page.module.css";

// The API's message (wrong e-mail, business account, expired link) shows above the button
export default function AcceptForm({ code }: { code: string }) {
    const [state, formAction] = useActionState<FormState, FormData>(acceptStaffInviteAction, {});
    return (
        <form action={formAction}>
            {state.error && (
                <p className={panelStyles.error} role="alert">
                    {state.error}
                </p>
            )}
            <input type="hidden" name="kode" value={code}/>
            <SubmitButton className={panelStyles.primaryButton} pendingText="Kobler til…">
                Godta og se arbeidsplanen
            </SubmitButton>
        </form>
    );
}
