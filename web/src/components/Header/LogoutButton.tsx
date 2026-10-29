"use client";
import { logoutAction } from "@/app/logg-inn/actions";
import { removePushAction } from "@/app/varsler/actions";

// "Logg ut" also turns off push messages on this device: the next person on a shared computer or the salon's
// tablet must not get this account's notifications (customer names, times)
export default function LogoutButton({ className }: { className: string }) {
    async function logout() {
        try {
            // getRegistration, not .ready: .ready never answers when BESTI has no service worker here
            const registration = "serviceWorker" in navigator ? await navigator.serviceWorker.getRegistration() : undefined;
            const subscription = await registration?.pushManager.getSubscription();
            if (subscription) {
                await removePushAction(subscription.endpoint);
                await subscription.unsubscribe();
            }
        } catch {
            // No push here, or the API did not answer: logging out must work anyway
        }
        await logoutAction();
    }

    return (
        <form action={logout}>
            <button type="submit" className={className}>Logg ut</button>
        </form>
    );
}
