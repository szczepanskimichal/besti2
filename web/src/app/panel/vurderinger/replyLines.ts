import type { ReactionKind } from "@/lib/types";

// Ready answers to a review, each with a crystal icon from the BESTI-chat. The answer is saved as text only,
// so the crystal shows in "Ditt svar" when the saved text is exactly one of these lines
export const REPLY_LINES: { text: string; icon: ReactionKind }[] = [
    { text: "Tusen takk!", icon: "hjerte" },
    { text: "Velkommen tilbake!", icon: "feiring" },
    { text: "Så hyggelig å høre!", icon: "stjerne" },
    { text: "Takk for tilbakemeldingen", icon: "tommel" },
    { text: "Beklager, det skal vi bli bedre på", icon: "trist" },
];

export function replyIcon(text: string | null) {
    return REPLY_LINES.find((line) => line.text === text?.trim())?.icon ?? null;
}
