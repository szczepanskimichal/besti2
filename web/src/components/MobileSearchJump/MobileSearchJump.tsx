"use client";
import {useEffect} from "react";

// Only on a phone/tablet (the hero and the search card are stacked there); on a computer nothing changes
const MOBILE = "(max-width: 899px)";
// Set when "Finn BESTI-time" is tapped on a phone, so the page jumps to the results after the new search loads
let jumpAfterSearch = false;

function scrollToId(id: string) {
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    document.getElementById(id)?.scrollIntoView({behavior: reduce ? "auto" : "smooth", block: "start"});
}

// The button under the title on a phone: one tap and the page slides down to the search card
export function JumpToSearchButton({className}: { className?: string }) {
    return (
        <button type="button" className={className} onClick={() => scrollToId("sok")}>
            Søk etter BESTI-time <span aria-hidden="true">↓</span>
        </button>
    );
}

// After "Finn BESTI-time" on a phone the page slides down to the results.
// search = the current search; when it changes, the new results are on their way
export function ScrollToResults({search}: { search: string }) {
    useEffect(() => {
        const form = document.getElementById("sok");
        const onSubmit = () => {
            if (!window.matchMedia(MOBILE).matches) return;
            jumpAfterSearch = true;
            scrollToId("resultater");
        };
        form?.addEventListener("submit", onSubmit);
        return () => form?.removeEventListener("submit", onSubmit);
    }, []);

    useEffect(() => {
        if (!jumpAfterSearch) return;
        jumpAfterSearch = false;
        // Wait one frame: the page may first scroll itself after the navigation
        requestAnimationFrame(() => scrollToId("resultater"));
    }, [search]);

    return null;
}
