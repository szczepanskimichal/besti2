import SubmitButton from "@/components/SubmitButton/SubmitButton";
import form from "../tjenester/page.module.css";
import { saveLoyaltyAction } from "./actions";

const options = [5, 6, 8, 10, 12];

// How many visits fill the card and what the customer gets
export default function LoyaltyForm({ visits, reward }: { visits: number | null; reward: string | null }) {
    return (
        <form action={saveLoyaltyAction} className={form.form}>
            <label className={form.field}>
                Antall besøk for en belønning
                <select name="visits" defaultValue={String(visits ?? 10)}>
                    {options.map((n) => <option key={n} value={n}>Hvert {n}. besøk</option>)}
                </select>
            </label>
            <label className={form.field}>
                Belønning
                <input name="reward" required maxLength={100} defaultValue={reward ?? ""}
                       placeholder="F.eks. «Gratis herreklipp» eller «50 % på neste behandling»"/>
            </label>
            <SubmitButton className={form.primaryButton} pendingText="Lagrer …">
                {visits ? "Lagre endringer" : "Slå på stempelkort"}
            </SubmitButton>
        </form>
    );
}
