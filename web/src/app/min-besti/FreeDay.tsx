"use client";

import Link from "next/link";
import { useLayoutEffect, useRef } from "react";
import empty from "@/components/EmptyState/EmptyState.module.css";
import styles from "./FreeDay.module.css";

// Ada 2026-11-03: "Helt ledig dag" with a crystal aurora magnifying glass.
// The glass glides over the title (the letters under the lens grow), then flies up and stays in the neon ring.
export default function FreeDay() {
    const title = "Helt ledig dag";
    const box = useRef<HTMLDivElement>(null);
    const heading = useRef<HTMLParagraphElement>(null);
    const flying = useRef<HTMLImageElement>(null);
    const resting = useRef<HTMLImageElement>(null);

    // Measure where the glass must fly: from the middle of the title to the glass in the ring
    useLayoutEffect(() => {
        const measure = () => {
            if (!box.current || !heading.current || !flying.current || !resting.current) return;
            const from = heading.current.getBoundingClientRect();
            const to = resting.current.getBoundingClientRect();
            const glass = flying.current.offsetWidth;
            box.current.style.setProperty("--fly-x", `${to.left + to.width * 0.4 - (from.left + from.width / 2)}px`);
            box.current.style.setProperty("--fly-y", `${to.top + to.height * 0.4 - (from.top + from.height / 2)}px`);
            box.current.style.setProperty("--fly-scale", `${glass > 0 ? to.width / glass : 0.4}`);
        };
        measure();
        // the Syne title changes size when the font arrives
        document.fonts?.ready.then(measure);
        window.addEventListener("resize", measure);
        return () => window.removeEventListener("resize", measure);
    }, []);

    return (
        <div ref={box} className={`${empty.box} ${styles.box}`}>
            <span className={`${empty.ring} ${styles.ring}`} aria-hidden="true">
                <span className={styles.sparkles}>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${empty.sparkle} ${empty.big}`}/>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${empty.sparkle} ${empty.small}`}/>
                    {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                    <img src="/ikoner/iskra.svg" alt="" className={`${empty.sparkle} ${empty.tiny}`}/>
                </span>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img ref={resting} src="/ikoner/lupe.svg" alt="" className={styles.ringGlass}/>
            </span>
            <p ref={heading} className={`${empty.title} ${styles.title}`}>
                {title}
                <span className={styles.zoom} aria-hidden="true">{title}</span>
                {/* eslint-disable-next-line @next/next/no-img-element -- small SVG icon, next/image not needed */}
                <img ref={flying} src="/ikoner/lupe.svg" alt="" className={styles.glass}/>
            </p>
            <p className={empty.text}>Trenger du en time? Finn en ledig tid hos BESTI.</p>
            <Link href="/" className={empty.action}>Finn en bedrift</Link>
        </div>
    );
}
