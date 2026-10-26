"use client";

import { useEffect, useRef } from "react";
import styles from "./Aurora.module.css";

// Curtains of thin vertical light rays along a slowly waving line (Ada's approved preview)
const curtains = [
    { y: 0.42, amp: 0.06, len: 0.3, hue: 152, speed: 1.0, phase: 0 },
    { y: 0.3, amp: 0.05, len: 0.24, hue: 165, speed: 0.7, phase: 2 },
    { y: 0.52, amp: 0.04, len: 0.2, hue: 145, speed: 1.3, phase: 4 },
    { y: 0.24, amp: 0.05, len: 0.16, hue: 290, speed: 0.9, phase: 6 },
];

// Smooth "random" wave made of three sine waves
function wave(x: number) {
    return Math.sin(x) * 0.5 + Math.sin(x * 2.3 + 1.7) * 0.3 + Math.sin(x * 5.1 + 0.3) * 0.2;
}

export default function Aurora() {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    // Moves on every page (still with prefers-reduced-motion or a hidden tab)
    const moving = true;

    useEffect(() => {
        const canvas = canvasRef.current;
        const ctx = canvas?.getContext("2d");
        if (!canvas || !ctx) return;
        const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        let t = 0;
        let frame = 0;

        const draw = () => {
            const w = canvas.width;
            const h = canvas.height;
            ctx.clearRect(0, 0, w, h);
            ctx.globalCompositeOperation = "lighter";
            for (const c of curtains) {
                for (let x = 0; x < w; x += 2) {
                    const u = x / w;
                    const base = h * (c.y + c.amp * wave(u * 4 + t * 0.15 * c.speed + c.phase));
                    const ray = Math.max(0, wave(u * 40 + t * 0.6 * c.speed + c.phase) * 0.6 + wave(u * 9 - t * 0.2) * 0.5 + 0.35);
                    const len = h * c.len * (0.8 + ray * 0.9);
                    const a = Math.min(1, ray * 0.5);
                    const g = ctx.createLinearGradient(0, base, 0, base - len);
                    g.addColorStop(0, `hsla(${c.hue}, 85%, 62%, ${a})`);
                    g.addColorStop(0.12, `hsla(${c.hue}, 80%, 55%, ${a * 0.8})`);
                    g.addColorStop(0.6, `hsla(${c.hue + 15}, 75%, 50%, ${a * 0.35})`);
                    g.addColorStop(1, "hsla(185, 70%, 45%, 0)");
                    ctx.fillStyle = g;
                    ctx.fillRect(x, base - len, 2, len + 3);
                }
            }
        };

        const loop = () => {
            draw();
            t += 0.012;
            frame = requestAnimationFrame(loop);
        };

        // Animate only when allowed and the tab is visible; otherwise draw one still frame
        const start = () => {
            cancelAnimationFrame(frame);
            if (moving && !reduceMotion && !document.hidden) loop();
            else draw();
        };

        // Half resolution is enough because the canvas is blurred anyway
        const resize = () => {
            canvas.width = Math.round(window.innerWidth * 0.5);
            canvas.height = Math.round(window.innerHeight * 0.5);
            start();
        };

        resize();
        window.addEventListener("resize", resize);
        document.addEventListener("visibilitychange", start);
        return () => {
            cancelAnimationFrame(frame);
            window.removeEventListener("resize", resize);
            document.removeEventListener("visibilitychange", start);
        };
    }, [moving]);

    return (
        <div className={styles.sky} aria-hidden="true">
            <div className={styles.stars} />
            <canvas ref={canvasRef} className={styles.canvas} />
            <svg className={styles.fjell} viewBox="0 0 1200 200" preserveAspectRatio="none">
                <path d="M0 200 L0 150 L90 120 L170 140 L260 90 L330 120 L420 70 L520 130 L600 105 L690 140 L780 95 L860 125 L950 80 L1040 130 L1120 110 L1200 135 L1200 200 Z" />
            </svg>
        </div>
    );
}
