import { ImageResponse } from "next/og";
import { getInvite } from "@/lib/api";
import { formatDate, formatTime } from "@/lib/format";

// The picture in the chat when the link is shared: aurora sky, two circles, who invites, to what and
// (for a group) how many places are left
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";
export const alt = "Invitasjon fra BESTI";

export default async function Image({ params }: { params: Promise<{ token: string }> }) {
    const { token } = await params;
    const invite = /^[A-Za-z0-9_-]{20,100}$/.test(token) ? await getInvite(token).catch(() => null) : null;
    const title = invite ? `${invite.inviterFirstName} inviterer deg` : "Bestill med din besti";
    const what = invite ? `${invite.serviceName} hos ${invite.businessName}` : "BESTI sammen";
    const group = invite && (invite.persons > 2 || !invite.organizerJoins);
    const places = group && invite.open ? ` · ${invite.placesLeft} av ${invite.persons} plasser ledige` : "";
    const when = invite ? `${formatDate(invite.startUtc)} kl. ${formatTime(invite.startUtc)}${places}` : "";

    return new ImageResponse(
        (
            <div style={{
                width: "100%", height: "100%", display: "flex", flexDirection: "column", justifyContent: "center",
                padding: "72px 88px", color: "#eef7f4", fontSize: 40,
                backgroundColor: "#050d1c",
                backgroundImage: "radial-gradient(60% 50% at 25% 20%, rgba(98, 195, 238, 0.45), transparent 70%), " +
                    "radial-gradient(50% 45% at 80% 15%, rgba(60, 200, 210, 0.30), transparent 70%), " +
                    "linear-gradient(180deg, #123a6b 0%, #050d1c 100%)",
            }}>
                <div style={{ display: "flex", marginBottom: 40 }}>
                    <div style={{
                        width: 120, height: 120, borderRadius: 60, border: "4px solid #62c3ee",
                        backgroundImage: "radial-gradient(circle at 40% 60%, rgba(98, 195, 238, 0.9), #06202f 70%)",
                    }}/>
                    <div style={{
                        width: 120, height: 120, borderRadius: 60, border: "4px solid #62c3ee", marginLeft: -36,
                        backgroundImage: "radial-gradient(circle at 60% 40%, rgba(60, 200, 210, 0.9), #06202f 70%)",
                    }}/>
                </div>
                <div style={{ fontSize: 76, fontWeight: 800, color: "#eef7f4" }}>{title}</div>
                <div style={{ marginTop: 20 }}>{what}</div>
                {when && <div style={{ marginTop: 8, color: "#c3dad3" }}>{when}</div>}
                <div style={{ marginTop: "auto", fontSize: 32, color: "#62c3ee" }}>bestiapp.no · Bestill med din besti</div>
            </div>
        ),
        size,
    );
}
