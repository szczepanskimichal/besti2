import { redirect } from "next/navigation";
import { getMe } from "@/lib/api";
import { getToken } from "@/lib/auth";

// Every panel page is for business accounts only: a customer goes to Min Besti, a visitor to login
export default async function PanelLayout({ children }: { children: React.ReactNode }) {
    const token = await getToken();
    const me = token ? await getMe(token) : null;
    if (!me) {
        redirect("/logg-inn");
    }
    if (!me.isBusiness) {
        redirect("/min-besti");
    }
    return children;
}