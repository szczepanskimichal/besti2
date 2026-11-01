"use client";
import { useEffect, useRef, useState } from "react";
import styles from "./IntroSplash.module.css";

// Ada 2026-11-01: when the app opens, the letters B-e-s-t-i light up one by one, break into sparks
// and the sparks fly together into the pin-B logo, over a slowly turning green aurora (about 4 seconds).
// Shown once per opening of the app (sessionStorage, set by INTRO_SCRIPT in the layout before the page is drawn),
// never with "reduce motion", and a tap anywhere skips it. The page loads underneath in the meantime.

// Runs in <head> before the first paint: decides if the intro is shown, so it never flashes in or out
export const INTRO_SCRIPT = `try{var h=document.documentElement;if(sessionStorage.getItem("besti-intro")||matchMedia("(prefers-reduced-motion: reduce)").matches){h.dataset.intro="off"}else{sessionStorage.setItem("besti-intro","1")}}catch(e){document.documentElement.dataset.intro="off"}`;

// Each letter's place inside the 1040 x 430 word (in %), cut tight so the files stay small
const LETTERS = [
    { left: 0.769, top: 2.326, width: 30.0, height: 95.349 },
    { left: 29.327, top: 1.395, width: 24.231, height: 96.279 },
    { left: 52.019, top: 3.023, width: 21.058, height: 94.419 },
    { left: 71.635, top: 14.186, width: 18.173, height: 81.628 },
    { left: 88.269, top: 10.233, width: 10.096, height: 83.256 },
];

const WORD_OUT = 1.85; // seconds: the word breaks into sparks
const LOGO_IN = 3.45; // the sparks have become the logo
const DONE = 4.1; // the intro fades away
const SPARKS = 420;
const ORBIT = 44;

type Point = [number, number];

// Random points inside the visible (non-transparent) part of a picture, as 0..1 of its size
function sample(draw: (g: CanvasRenderingContext2D) => void, width: number, height: number, count: number): Point[] {
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const g = canvas.getContext("2d");
    if (!g) return [];
    draw(g);
    const data = g.getImageData(0, 0, width, height).data;
    const points: Point[] = [];
    for (let tries = 0; points.length < count && tries < 400000; tries++) {
        const x = Math.random() * width | 0;
        const y = Math.random() * height | 0;
        if (data[(y * width + x) * 4 + 3] > 150) points.push([x / width, y / height]);
    }
    return points;
}

function loaded(image: HTMLImageElement): Promise<void> {
    return image.complete ? Promise.resolve() : image.decode().catch(() => undefined);
}

export default function IntroSplash() {
    const [gone, setGone] = useState(false);
    const [leaving, setLeaving] = useState(false);
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const stageRef = useRef<HTMLDivElement>(null);
    const wordRef = useRef<HTMLDivElement>(null);
    const iconRef = useRef<HTMLImageElement>(null);

    useEffect(() => {
        const html = document.documentElement;
        // Hidden by CSS already (and its lazy pictures are never downloaded): nothing to animate
        if (html.dataset.intro === "off") return;
        const canvas = canvasRef.current!;
        const stage = stageRef.current!;
        const word = wordRef.current!;
        const icon = iconRef.current!;
        const x = canvas.getContext("2d");
        if (!x) return;

        let width = 0, height = 0;
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        const resize = () => {
            width = window.innerWidth;
            height = window.innerHeight;
            canvas.width = width * dpr;
            canvas.height = height * dpr;
            x.setTransform(dpr, 0, 0, dpr, 0, 0);
        };
        resize();
        window.addEventListener("resize", resize);

        const orbit = Array.from({ length: ORBIT }, () => ({
            a: Math.random() * 6.283, r: .75 + Math.random() * .5,
            v: (.25 + Math.random() * .45) * (Math.random() < .8 ? 1 : -1),
            s: .8 + Math.random() * 1.7, tw: Math.random() * 6.283, yo: (Math.random() - .5) * .25,
        }));
        const swirl = Array.from({ length: SPARKS }, () => ({
            a: Math.random() * 6.283, r: .15 + Math.random() * .35, s: .6 + Math.random() * 1.2, dl: Math.random() * .35,
        }));
        let from: Point[] = [];
        let to: Point[] = [];

        // The sparks start where the letters are and end where the logo is
        const letters = Array.from(word.querySelectorAll("img"));
        Promise.all([...letters, icon].map(loaded)).then(() => {
            from = sample((g) => letters.forEach((img, k) => {
                const l = LETTERS[k];
                g.drawImage(img, l.left * 10.4, l.top * 4.3, l.width * 10.4, l.height * 4.3);
            }), 1040, 430, SPARKS);
            to = sample((g) => g.drawImage(icon, 0, 0, 265, 360), 265, 360, SPARKS);
        });

        const dot = (px: number, py: number, r: number, alpha: number) => {
            const g = x.createRadialGradient(px, py, 0, px, py, r);
            g.addColorStop(0, `rgba(225,255,246,${alpha})`);
            g.addColorStop(.35, `rgba(132,228,204,${alpha * .5})`);
            g.addColorStop(1, "rgba(132,228,204,0)");
            x.fillStyle = g;
            x.beginPath();
            x.arc(px, py, r, 0, 6.283);
            x.fill();
        };
        const ease = (u: number) => u < .5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
        const clamp = (u: number) => Math.max(0, Math.min(1, u));

        const start = performance.now();
        let frame = 0;
        let wordOut = false, logoIn = false, leavingNow = false;
        const draw = (now: number) => {
            const t = (now - start) / 1000;
            x.clearRect(0, 0, width, height);
            x.globalCompositeOperation = "lighter";
            const r = stage.getBoundingClientRect();
            const w = word.getBoundingClientRect();
            const cx = r.left + r.width / 2, cy = r.top + r.height / 2;
            const iw = icon.offsetWidth, ih = icon.offsetHeight;
            if (t > WORD_OUT && !wordOut) {
                wordOut = true;
                word.classList.add(styles.wordOut);
            }
            if (t > LOGO_IN - .35 && !logoIn) {
                logoIn = true;
                icon.classList.add(styles.iconIn);
            }
            if (t > WORD_OUT - .1 && from.length && to.length) {
                for (let k = 0; k < Math.min(from.length, to.length); k++) {
                    const q = swirl[k];
                    const e = ease(clamp((t - WORD_OUT - q.dl) / (LOGO_IN - WORD_OUT - .35)));
                    const sx = w.left + from[k][0] * w.width, sy = w.top + from[k][1] * w.height;
                    const tx = cx + (to[k][0] - .5) * iw, ty = cy + (to[k][1] - .5) * ih;
                    const spread = Math.sin(Math.PI * e), angle = q.a + e * 5;
                    dot(sx + (tx - sx) * e + Math.cos(angle) * r.width * q.r * spread,
                        sy + (ty - sy) * e + Math.sin(angle) * r.width * q.r * spread * .7,
                        q.s * (2 + 3 * spread),
                        .8 * Math.min(1, (t - WORD_OUT + .1) / .3) * (1 - clamp((t - LOGO_IN) / .5)));
                }
            }
            const on = clamp((t - .3) / 1.2) * (t > WORD_OUT && t < LOGO_IN ? .5 : 1);
            for (const q of orbit) {
                for (let j = 5; j >= 0; j--) {
                    const a = q.a + q.v * (t - j * .035);
                    const twinkle = .55 + .45 * Math.sin(t * 3 + q.tw);
                    dot(cx + Math.cos(a) * r.width * .66 * q.r, cy + Math.sin(a) * r.height * .42 * q.r + q.yo * r.height * .42,
                        q.s * (j ? 3 : 6), on * twinkle * (j ? .35 * (1 - j / 5) : 1));
                }
            }
            if (t > DONE && !leavingNow) {
                leavingNow = true;
                setLeaving(true);
            }
            frame = requestAnimationFrame(draw);
        };
        frame = requestAnimationFrame(draw);

        return () => {
            cancelAnimationFrame(frame);
            window.removeEventListener("resize", resize);
        };
    }, []);

    // Fading away (by itself at the end or after a tap): gone for good when the fade is over
    useEffect(() => {
        if (!leaving) return;
        const timer = window.setTimeout(() => {
            document.documentElement.dataset.intro = "off";
            setGone(true);
        }, 450);
        return () => window.clearTimeout(timer);
    }, [leaving]);

    if (gone) return null;

    const skip = () => setLeaving(true);

    return (
        <div className={leaving ? `${styles.intro} ${styles.leaving}` : styles.intro}>
            <div className={styles.aurora} aria-hidden="true"><i className={styles.a1}/><i className={styles.a2}/><i className={styles.a3}/></div>
            <canvas ref={canvasRef} className={styles.canvas} aria-hidden="true"/>
            <div ref={stageRef} className={styles.stage} aria-hidden="true">
                <div ref={wordRef} className={styles.word}>
                    {LETTERS.map((l, k) => (
                        // eslint-disable-next-line @next/next/no-img-element -- transparent layers drawn on a canvas too
                        <img key={k} src={`/intro/litera-${k + 1}.webp`} alt="" className={styles.letter} loading="lazy" fetchPriority="high"
                             style={{ left: `${l.left}%`, top: `${l.top}%`, width: `${l.width}%`, height: `${l.height}%`,
                                 animationDelay: `${.25 + k * .24}s` }}/>
                    ))}
                </div>
                {/* eslint-disable-next-line @next/next/no-img-element -- drawn on a canvas too */}
                <img ref={iconRef} src="/intro/ikon.webp" alt="" className={styles.icon} loading="lazy" fetchPriority="high"/>
            </div>
            <button type="button" className={styles.skip} onClick={skip} aria-label="Hopp over introen"/>
        </div>
    );
}
