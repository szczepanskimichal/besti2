"use client";

import { useEffect, useRef, useState, type CSSProperties } from "react";
import styles from "./Aurora.module.css";

// Brightest stars in the photo as [x, y, brightness], x and y as a share of the image size.
// Only these twinkle; the photo is drawn with background-size: cover, so they are placed the same way.
const STARS = {
    desktop: { w: 2400, h: 1350, s: [[0.1083, 0.0156, 1.0], [0.7588, 0.4933, 0.99], [0.0583, 0.3867, 0.96], [0.4079, 0.7756, 0.96], [0.69, 0.6533, 0.95], [0.8425, 0.577, 0.94], [0.0183, 0.96, 0.94], [0.6817, 0.5393, 0.93], [0.2221, 0.1022, 0.93], [0.5354, 0.7281, 0.92], [0.4129, 0.0356, 0.91], [0.6029, 0.7793, 0.91], [0.63, 0.6711, 0.89], [0.0396, 0.1741, 0.89], [0.4421, 0.5207, 0.87], [0.9129, 0.3696, 0.85], [0.2475, 0.0111, 0.84], [0.1825, 0.3059, 0.84], [0.5996, 0.5363, 0.84], [0.8213, 0.7889, 0.83], [0.2942, 0.1704, 0.83], [0.0171, 0.4778, 0.83], [0.4892, 0.5822, 0.83], [0.2329, 0.9852, 0.82], [0.68, 0.4444, 0.82], [0.3658, 0.6459, 0.8]] },
    mobil: { w: 1000, h: 2000, s: [[0.196, 0.0145, 1.0], [0.575, 0.0955, 0.99], [0.028, 0.363, 0.96], [0.659, 0.0105, 0.92], [0.814, 0.1595, 0.88], [0.075, 0.3725, 0.87], [0.489, 0.218, 0.87], [0.314, 0.2315, 0.87], [0.935, 0.675, 0.86], [0.177, 0.327, 0.86], [0.443, 0.287, 0.85], [0.551, 0.3135, 0.83], [0.103, 0.338, 0.82], [0.35, 0.2125, 0.82], [0.611, 0.9235, 0.82], [0.688, 0.134, 0.82], [0.923, 0.114, 0.82], [0.506, 0.253, 0.81], [0.85, 0.62, 0.81], [0.33, 0.075, 0.8], [0.916, 0.6235, 0.8], [0.257, 0.1485, 0.78], [0.352, 0.339, 0.78], [0.041, 0.2945, 0.77], [0.767, 0.01, 0.76], [0.269, 0.4535, 0.75]] },
};

type Size = { w: number; h: number };

export default function Aurora() {
    const svgRef = useRef<SVGSVGElement>(null);
    const skyRef = useRef<HTMLDivElement>(null);
    // Unknown on the server, so the stars appear right after the page loads
    const [size, setSize] = useState<Size | null>(null);

    useEffect(() => {
        // The SVG wave animation is not CSS, so it is paused by hand for people who turn off motion
        const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        if (reducedMotion) svgRef.current?.pauseAnimations();
        // Phones resize the window while scrolling (the address bar hides), so only a new width moves the stars
        const resize = () => setSize((old) => old?.w === window.innerWidth ? old : { w: window.innerWidth, h: window.innerHeight });
        resize();
        // The sky stands still while the page scrolls: old phones cannot redraw the moving sky
        // behind the frosted cards and scroll smoothly at the same time
        let timer: number | undefined;
        const scroll = () => {
            if (timer === undefined) {
                skyRef.current?.classList.add(styles.scrolling);
                if (!reducedMotion) svgRef.current?.pauseAnimations();
            }
            window.clearTimeout(timer);
            timer = window.setTimeout(() => {
                timer = undefined;
                skyRef.current?.classList.remove(styles.scrolling);
                if (!reducedMotion) svgRef.current?.unpauseAnimations();
            }, 250);
        };
        window.addEventListener("resize", resize);
        window.addEventListener("scroll", scroll, { passive: true });
        return () => {
            window.removeEventListener("resize", resize);
            window.removeEventListener("scroll", scroll);
            window.clearTimeout(timer);
        };
    }, []);

    return (
        <div ref={skyRef} className={styles.sky} aria-hidden="true">
            <svg ref={svgRef} width="0" height="0" style={{ position: "absolute" }}>
                <filter id="besti-warp" x="-10%" y="-10%" width="120%" height="120%">
                    <feTurbulence type="fractalNoise" baseFrequency="0.0025 0.008" numOctaves={2} seed={4} result="n">
                        <animate attributeName="baseFrequency" dur="14s" values="0.002 0.006;0.0045 0.010;0.002 0.006" repeatCount="indefinite"/>
                    </feTurbulence>
                    <feDisplacementMap in="SourceGraphic" in2="n" scale={95} xChannelSelector="R" yChannelSelector="G"/>
                </filter>
            </svg>
            <i className={styles.photo}/>
            <i className={styles.aurora}/>
            <div className={styles.stars}>{size && <Stars size={size}/>}</div>
            <div className={styles.shade}/>
        </div>
    );
}

function Stars({ size }: { size: Size }) {
    const desktop = size.w >= 800;
    const image = desktop ? STARS.desktop : STARS.mobil;
    // Same maths as background-size: cover (centered on a computer, from the top on a phone)
    const scale = Math.max(size.w / image.w, size.h / image.h);
    const width = image.w * scale;
    const height = image.h * scale;
    const left = (size.w - width) / 2;
    const top = desktop ? (size.h - height) / 2 : 0;

    return image.s.map(([x, y, brightness], i) => (
        <u
            key={i}
            className={i % 2 === 0 ? `${styles.star} ${styles.big}` : styles.star}
            style={{
                left: left + x * width,
                top: top + y * height,
                "--d": `${4 + ((i * 37) % 50) / 10}s`,
                "--dl": `${-((i * 53) % 70) / 10}s`,
                "--o": (0.55 + 0.4 * brightness).toFixed(2),
            } as CSSProperties}
        />
    ));
}
