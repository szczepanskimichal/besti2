"use client";
import { useEffect, useRef, useState } from "react";
import styles from "./IntroSplash.module.css";

// Ada 2026-11-04 (intro v4): when the app opens, the B rises out of mint smoke, the polar star draws the
// neon frame around it, the logo slides to the left and B-E-S-T-I is written next to it, letter by letter,
// with fine star dust orbiting around (about 4.6 seconds), over a slowly turning green aurora.
// Shown once per opening of the app (sessionStorage, set by INTRO_SCRIPT in the layout before the page is drawn),
// never with "reduce motion", and a tap anywhere skips it. The page loads underneath in the meantime.

// Runs in <head> before the first paint: decides if the intro is shown, so it never flashes in or out.
// Only when the app opens on the front page: a customer from a QR code, Instagram or a BESTI sammen link
// goes straight to the business or the invitation (and does not get the intro later in the same visit)
export const INTRO_SCRIPT = `try{var h=document.documentElement;if(sessionStorage.getItem("besti-intro")||location.pathname!=="/"||matchMedia("(prefers-reduced-motion: reduce)").matches){h.dataset.intro="off"}sessionStorage.setItem("besti-intro","1")}catch(e){document.documentElement.dataset.intro="off"}`;

// Timeline in seconds
const B_IN = 0.1; // the B comes out of the smoke
const STAR_IN = 0.85; // the polar star is born
const DRAW_START = 1.0; // the star starts drawing the frame
const DRAW_END = 2.2; // the frame is closed
const SLIDE_START = 2.4; // the logo slides to the left
const SLIDE_END = 2.9;
const WORD_START = 2.62; // first letter
const LETTER_STEP = 0.16; // next letter
const DONE = 4.6; // the intro fades away

// The same shapes as public/logo/besti-f1.svg (a 100 x 100 square, frame from 4 to 96)
const B_PATH = "M29 19 H53.5 C65.5 19 72 25.6 72 34.3 C72 41 68.2 45.8 62.6 47.8 C70.4 49.8 75.8 55.9 75.8 64.3 C75.8 75 68.4 81.5 56.4 81.5 H29 Z M40.6 28.6 H52 C57.6 28.6 60.4 31.6 60.4 35.6 C60.4 39.6 57.6 42.6 52 42.6 H40.6 Z M40.6 52.2 H55 C60.8 52.2 64 55.6 64 61.2 C64 66.8 60.8 71.9 55 71.9 H40.6 Z";
const MINT_FRAME = "#72f2cf";
const COLOURS: [number, number, number][] = [[143, 240, 214], [111, 240, 168], [79, 209, 197], [190, 255, 236], [60, 220, 160], [150, 230, 255]];

type Rgb = [number, number, number];
type Particle = {
    kind: "dust" | "smoke" | "star" | "glint";
    x: number; y: number; vx: number; vy: number;
    size: number; grow: number; life: number; age: number; alpha: number;
    rot: number; spin: number; colour: Rgb; phase: number; noDust?: boolean;
};

const clamp = (v: number) => Math.max(0, Math.min(1, v));
const easeInOut = (u: number) => u < .5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
const easeOut = (u: number) => 1 - Math.pow(1 - u, 3);
const randomColour = () => COLOURS[Math.random() * COLOURS.length | 0];

// Points along the rounded frame, starting at the gap where the star sits (top right corner)
function framePoints(): [number, number][] {
    const points: [number, number][] = [];
    const r = 22, x0 = 4, y0 = 4, x1 = 96, y1 = 96;
    const arc = (cx: number, cy: number, a0: number, a1: number, n: number) => {
        for (let i = 0; i <= n; i++) {
            const a = a0 + (a1 - a0) * i / n;
            points.push([cx + r * Math.cos(a), cy + r * Math.sin(a)]);
        }
    };
    const line = (ax: number, ay: number, bx: number, by: number, n: number) => {
        for (let i = 1; i < n; i++) points.push([ax + (bx - ax) * i / n, ay + (by - ay) * i / n]);
    };
    arc(x1 - r, y0 + r, -Math.PI / 4, 0, 12);
    line(x1, y0 + r, x1, y1 - r, 40);
    arc(x1 - r, y1 - r, 0, Math.PI / 2, 24);
    line(x1 - r, y1, x0 + r, y1, 40);
    arc(x0 + r, y1 - r, Math.PI / 2, Math.PI, 24);
    line(x0, y1 - r, x0, y0 + r, 40);
    arc(x0 + r, y0 + r, Math.PI, 1.5 * Math.PI, 24);
    line(x0 + r, y0, x1 - r, y0, 40);
    arc(x1 - r, y0 + r, 1.5 * Math.PI, 1.75 * Math.PI, 12);
    return points;
}

export default function IntroSplash() {
    const [gone, setGone] = useState(false);
    const [leaving, setLeaving] = useState(false);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const html = document.documentElement;
        // Hidden by CSS already: nothing to animate
        if (html.dataset.intro === "off") return;
        const canvas = canvasRef.current!;
        const x = canvas.getContext("2d");
        if (!x) return;

        // Everything is drawn in a 600 x 340 "stage" in the middle of the screen, scaled to fit
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        let scale = 1, offsetX = 0, offsetY = 0;
        // One extra canvas per layer that gets blurred or smoked: the B, the 5 letters and the frame
        const layers = Array.from({ length: 7 }, () => document.createElement("canvas"));
        const resize = () => {
            const width = window.innerWidth, height = window.innerHeight;
            canvas.width = width * dpr;
            canvas.height = height * dpr;
            const stageWidth = Math.min(width * .96, 860);
            scale = stageWidth / 600;
            offsetX = (width - stageWidth) / 2;
            offsetY = height / 2 - 170 * scale;
            for (const layer of layers) {
                layer.width = canvas.width;
                layer.height = canvas.height;
            }
        };
        resize();
        window.addEventListener("resize", resize);

        const bPath = new Path2D(B_PATH);
        const frame = framePoints();
        const starImage = new Image();
        starImage.src = "/ikoner/iskra.svg";
        // The font of the BESTI word in the header (Montserrat 700 from the layout)
        const font = getComputedStyle(html).getPropertyValue("--font-logo").trim() || "sans-serif";

        // Size and place of the logo and of each letter, once the font is ready
        let tile = 150, fontSize = 90, capHeight = .7, logoLeft = 0;
        let letters: { ch: string; x: number; w: number }[] = [];
        const layout = () => {
            tile = window.innerWidth < 600 ? 146 : 150;
            const g = layers[0].getContext("2d")!;
            g.font = `700 100px ${font}`;
            capHeight = g.measureText("E").actualBoundingBoxAscent / 100;
            fontSize = tile * .6;
            g.font = `700 ${fontSize}px ${font}`;
            const spacing = fontSize * .1;
            const widths = [..."BESTI"].map((ch) => g.measureText(ch).width);
            const wordWidth = widths.reduce((a, b) => a + b, 0) + spacing * 4;
            const gap = tile * .18;
            logoLeft = 300 - (tile * .92 + gap + wordWidth) / 2 - tile * .04;
            let left = logoLeft + tile * .96 + gap;
            letters = widths.map((w, i) => {
                const letter = { ch: "BESTI"[i], x: left, w };
                left += w + spacing;
                return letter;
            });
        };

        const particles: Particle[] = [];
        const add = (p: Partial<Particle>) => particles.push({
            kind: "dust", x: 0, y: 0, vx: 0, vy: 0, size: 2, grow: 0, life: 1, age: 0, alpha: 1,
            rot: Math.random() * 6.28, spin: 0, colour: COLOURS[0], phase: Math.random() * 6.28, ...p,
        });
        const glow = (g: CanvasRenderingContext2D, px: number, py: number, r: number, alpha: number, [R, G, B]: Rgb) => {
            const gr = g.createRadialGradient(px, py, 0, px, py, r);
            gr.addColorStop(0, `rgba(240,255,250,${alpha})`);
            gr.addColorStop(.3, `rgba(${R},${G},${B},${alpha * .7})`);
            gr.addColorStop(1, `rgba(${R},${G},${B},0)`);
            g.fillStyle = gr;
            g.beginPath();
            g.arc(px, py, r, 0, 6.283);
            g.fill();
        };
        const smoke = (px: number, py: number, r: number, alpha: number) => {
            const gr = x.createRadialGradient(px, py, 0, px, py, r);
            gr.addColorStop(0, `rgba(185,240,228,${alpha})`);
            gr.addColorStop(.5, `rgba(110,210,190,${alpha * .45})`);
            gr.addColorStop(1, "rgba(110,210,190,0)");
            x.fillStyle = gr;
            x.beginPath();
            x.arc(px, py, r, 0, 6.283);
            x.fill();
        };
        const star = (px: number, py: number, size: number, rot: number, alpha: number) => {
            if (!starImage.complete || size <= 0) return;
            x.save();
            x.globalAlpha = alpha;
            x.translate(px, py);
            x.rotate(rot);
            x.drawImage(starImage, -size / 2, -size / 2, size, size);
            x.restore();
        };
        const dust = (px: number, py: number, n: number, speed: number, up = 0) => {
            for (let j = 0; j < n; j++) {
                const a = Math.random() * 6.28, v = speed * (.2 + Math.random());
                add({ kind: "dust", x: px + (Math.random() - .5) * 6, y: py + (Math.random() - .5) * 6, vx: Math.cos(a) * v,
                    vy: Math.sin(a) * v - up, size: .5 + Math.random() * 1.2, life: .9 + Math.random() * 1.3, alpha: .95, colour: randomColour() });
            }
        };
        const puff = (px: number, py: number, w: number, h: number, n: number) => {
            for (let j = 0; j < n; j++) {
                add({ kind: "smoke", x: px + (Math.random() - .5) * w, y: py + (Math.random() - .5) * h, vx: (Math.random() - .5) * 24,
                    vy: -14 - Math.random() * 20, size: 8 + Math.random() * 12, grow: 20, life: .8 + Math.random() * .5, alpha: .14 });
            }
        };
        // Fine star dust orbiting around the logo the whole time
        const orbit = Array.from({ length: 90 }, () => ({
            a: Math.random() * 6.283, r: .75 + Math.random() * .5, v: (.25 + Math.random() * .45) * (Math.random() < .8 ? 1 : -1),
            s: .6 + Math.random() * 1.1, tw: Math.random() * 6.283, yo: (Math.random() - .5) * .25, colour: randomColour(),
        }));

        // Draws a mint shape on its own canvas (blurred while it appears, with moving smoke inside), then onto the screen
        let time = 0;
        const layer = (i: number, draw: (g: CanvasRenderingContext2D) => void, alpha: number, blur: number, smokiness: number) => {
            const lc = layers[i], g = lc.getContext("2d")!;
            g.setTransform(1, 0, 0, 1, 0, 0);
            g.clearRect(0, 0, lc.width, lc.height);
            g.setTransform(dpr * scale, 0, 0, dpr * scale, dpr * offsetX, dpr * offsetY);
            g.globalCompositeOperation = "source-over";
            const fill = g.createLinearGradient(0, 100, 0, 240);
            fill.addColorStop(0, "#a8f8e0");
            fill.addColorStop(1, "#7fe6c9");
            g.fillStyle = fill;
            draw(g);
            if (smokiness > 0) {
                g.globalCompositeOperation = "source-atop";
                for (let j = 0; j < 9; j++) {
                    const px = 60 + j * 60 + Math.sin(time + j) * 25, py = 170 + Math.cos(time * .8 + j * 1.7) * 35;
                    const gr = g.createRadialGradient(px, py, 0, px, py, 50);
                    gr.addColorStop(0, j % 2 ? `rgba(10,70,62,${.6 * smokiness})` : `rgba(255,255,255,${.55 * smokiness})`);
                    gr.addColorStop(1, "rgba(0,0,0,0)");
                    g.fillStyle = gr;
                    g.fillRect(px - 50, py - 50, 100, 100);
                }
            }
            x.save();
            x.setTransform(1, 0, 0, 1, 0, 0);
            x.globalAlpha = alpha;
            x.filter = blur > .2 ? `blur(${blur * scale * dpr}px)` : "none";
            x.shadowColor = "rgba(114,242,207,.55)";
            x.shadowBlur = (12 + blur * 2) * scale * dpr;
            x.drawImage(lc, 0, 0);
            x.restore();
        };

        const done: Record<string, boolean> = {};
        const once = (name: string, fn: () => void) => {
            if (!done[name]) {
                done[name] = true;
                fn();
            }
        };

        let start: number | null = null, last = 0, trail = 0, twinkle = 0, frameId = 0, leavingNow = false;
        const draw = (now: number) => {
            if (start === null) start = now;
            const t = (now - start) / 1000;
            const dt = Math.min(.05, Math.max(0, t - last));
            last = t;
            time = t;
            x.setTransform(1, 0, 0, 1, 0, 0);
            x.clearRect(0, 0, canvas.width, canvas.height);
            x.setTransform(dpr * scale, 0, 0, dpr * scale, dpr * offsetX, dpr * offsetY);

            // Orbiting dust
            x.globalCompositeOperation = "lighter";
            const on = clamp((t - .2) / 1.2) * .75;
            for (const q of orbit) {
                for (let j = 4; j >= 0; j--) {
                    const a = q.a + q.v * (t - j * .04);
                    const shine = .55 + .45 * Math.sin(t * 3 + q.tw);
                    glow(x, 300 + Math.cos(a) * 360 * q.r, 170 + Math.sin(a) * 150 * q.r + q.yo * 150,
                        q.s * (j ? .9 : 1.7), on * shine * (j ? .3 * (1 - j / 4) : 1), q.colour);
                }
            }
            x.globalCompositeOperation = "source-over";

            // Where the 100 x 100 logo square is right now (in the middle, then slid to the left)
            const size = tile / 100;
            const slide = easeInOut(clamp((t - SLIDE_START) / (SLIDE_END - SLIDE_START)));
            const bx = (300 - 50 * size) + (logoLeft - (300 - 50 * size)) * slide, by = 170 - 50 * size;
            const toStage = (px: number, py: number): [number, number] => [bx + px * size, by + py * size];
            const drawn = easeInOut(clamp((t - DRAW_START) / (DRAW_END - DRAW_START)));

            // Dark tile behind the B
            if (t > DRAW_START - .1) {
                x.save();
                x.translate(bx, by);
                x.scale(size, size);
                x.globalAlpha = clamp((t - DRAW_START) / 1.4);
                const bg = x.createRadialGradient(50, 45, 0, 50, 45, 62);
                bg.addColorStop(0, "#0a1c22");
                bg.addColorStop(1, "#02080c");
                x.fillStyle = bg;
                x.beginPath();
                x.roundRect(4, 4, 92, 92, 22);
                x.fill();
                x.restore();
            }

            // The neon frame, drawn behind the star, with the gap around the star cut out
            if (t > DRAW_START) {
                const lc = layers[6], g = lc.getContext("2d")!;
                g.setTransform(1, 0, 0, 1, 0, 0);
                g.clearRect(0, 0, lc.width, lc.height);
                g.setTransform(dpr * scale * size, 0, 0, dpr * scale * size, dpr * (offsetX + bx * scale), dpr * (offsetY + by * scale));
                const n = Math.floor(drawn * (frame.length - 1));
                if (n > 0) {
                    g.strokeStyle = MINT_FRAME;
                    g.lineWidth = 3.6;
                    g.lineCap = "round";
                    g.lineJoin = "round";
                    g.shadowColor = "rgba(114,242,207,.9)";
                    g.shadowBlur = 6 * scale * size * dpr;
                    g.beginPath();
                    g.moveTo(frame[0][0], frame[0][1]);
                    for (let i = 1; i <= n; i++) g.lineTo(frame[i][0], frame[i][1]);
                    g.stroke();
                    g.globalCompositeOperation = "destination-out";
                    g.beginPath();
                    g.arc(90, 10, 28, 0, 6.283);
                    g.fill();
                    g.globalCompositeOperation = "source-over";
                }
                x.save();
                x.setTransform(1, 0, 0, 1, 0, 0);
                x.drawImage(lc, 0, 0);
                x.restore();
            }

            // The B out of the smoke, with a short flash when the frame closes
            if (t > B_IN) {
                const e = easeOut(clamp((t - B_IN) / .75));
                once("b", () => {
                    const [cx, cy] = toStage(50, 47);
                    puff(cx, cy, 60, 70, 26);
                    dust(cx, cy, 45, 40, 10);
                });
                const placeB = (g: CanvasRenderingContext2D) => {
                    g.translate(bx, by);
                    g.scale(size, size);
                    g.translate(50, 47);
                    g.scale(.8, .8);
                    g.translate(-52.4, -50.25);
                };
                layer(0, (g) => {
                    g.save();
                    placeB(g);
                    g.fill(bPath, "evenodd");
                    g.restore();
                }, e, 16 * (1 - e), 1 - clamp((t - 1.2) / 1.2));
                const flash = t > DRAW_END && t < DRAW_END + .6 ? Math.sin(clamp((t - DRAW_END) / .6) * Math.PI) : 0;
                if (flash > 0) {
                    x.save();
                    x.globalCompositeOperation = "lighter";
                    x.globalAlpha = flash * .45;
                    placeB(x);
                    x.fillStyle = "rgba(190,255,235,.5)";
                    x.fill(bPath, "evenodd");
                    x.restore();
                }
            }

            // The polar star: born in the corner, runs once around drawing the frame, then rests in the gap
            if (t > STAR_IN) {
                const birth = easeOut(clamp((t - STAR_IN) / .3));
                let px: number, py: number, rot: number, starSize: number;
                if (t < DRAW_START) {
                    [px, py] = toStage(89.6, 10.4);
                    rot = (1 - birth) * 2;
                    starSize = 44 * size * .8 * birth;
                } else if (drawn < 1) {
                    const i = Math.floor(drawn * (frame.length - 1));
                    [px, py] = toStage(frame[i][0], frame[i][1]);
                    rot = drawn * Math.PI * 2;
                    starSize = 44 * size * .8;
                    trail += dt;
                    while (trail > .005) {
                        trail -= .005;
                        add({ kind: "dust", x: px + (Math.random() - .5) * 7, y: py + (Math.random() - .5) * 7, vx: (Math.random() - .5) * 22,
                            vy: (Math.random() - .5) * 16 - 10, size: .7 + Math.random() * 2, life: .9 + Math.random() * 1.1, alpha: .95, colour: randomColour() });
                        if (Math.random() < .05) add({ kind: "star", x: px, y: py, vx: (Math.random() - .5) * 50, vy: (Math.random() - .5) * 50 - 10,
                            size: 3 + Math.random() * 3, life: 1.1, spin: (Math.random() - .5) * 3 });
                    }
                } else {
                    [px, py] = toStage(90, 10);
                    rot = 0;
                    starSize = 44 * size * (.8 + .2 * easeOut(clamp((t - DRAW_END) / .35)));
                }
                once("star", () => dust(px, py, 60, 90));
                if (t > DRAW_END) once("closed", () => {
                    dust(px, py, 110, 130);
                    for (let j = 0; j < 6; j++) {
                        const a = Math.random() * 6.28;
                        add({ kind: "star", x: px + Math.cos(a) * 25, y: py + Math.sin(a) * 25, vx: Math.cos(a) * 45, vy: Math.sin(a) * 45,
                            size: 3.5 + Math.random() * 3.5, life: 1.3, spin: (Math.random() - .5) * 3 });
                    }
                });
                const flash = Math.max(t < STAR_IN + .5 ? 1 - clamp((t - STAR_IN) / .5) : 0, t > DRAW_END ? 1 - clamp((t - DRAW_END) / .6) : 0);
                x.save();
                x.globalCompositeOperation = "lighter";
                glow(x, px, py, starSize * (.9 + flash * 1.8), .3 + flash * .7, COLOURS[1]);
                x.restore();
                star(px, py, starSize, rot, 1);
            }

            // B-E-S-T-I written one by one, each letter out of its own little smoke
            for (let i = 0; i < letters.length; i++) {
                const from = WORD_START + i * LETTER_STEP;
                if (t < from) continue;
                const e = easeOut(clamp((t - from) / .55));
                const q = letters[i];
                once("letter" + i, () => {
                    puff(q.x + q.w / 2, 170, q.w, fontSize * capHeight, 10);
                    dust(q.x + q.w / 2, 170, 34, 45, 12);
                    if (Math.random() < .9) add({ kind: "star", x: q.x + q.w * Math.random(), y: 170 - fontSize * capHeight * .7,
                        vx: (Math.random() - .5) * 20, vy: -20, size: 3 + Math.random() * 2.5, life: 1, spin: (Math.random() - .5) * 2 });
                });
                layer(1 + i, (g) => {
                    g.font = `700 ${fontSize}px ${font}`;
                    g.fillText(q.ch, q.x, 170 + fontSize * capHeight / 2);
                }, e, 12 * (1 - e), 1 - clamp((t - from - .3) / .9));
            }

            // Small glints and stars twinkling around the finished logo
            if (t > DRAW_END) {
                twinkle += dt;
                while (twinkle > .03) {
                    twinkle -= .03;
                    const a = Math.random() * 6.283, rr = .78 + Math.random() * .45;
                    const px = 300 + Math.cos(a) * 300 * rr, py = 170 + Math.sin(a) * 118 * rr, v = 10 + Math.random() * 10;
                    if (Math.random() < .1) add({ kind: "star", x: px, y: py, vy: -3, size: 2.5 + Math.random() * 2, life: 1.2,
                        spin: Math.random() - .5, alpha: .9, noDust: true });
                    else add({ kind: "glint", x: px, y: py, vx: -Math.sin(a) * v * 2.5, vy: Math.cos(a) * v, size: .35 + Math.random() * .6,
                        life: 1 + Math.random() * .8, alpha: .9, colour: randomColour() });
                }
            }

            // Move and draw every particle
            for (let i = particles.length - 1; i >= 0; i--) {
                const p = particles[i];
                p.age += dt;
                if (p.age > p.life) {
                    particles.splice(i, 1);
                    continue;
                }
                p.x += p.vx * dt;
                p.y += p.vy * dt;
                p.vx *= .982;
                p.vy *= .982;
                if (p.kind === "dust") p.vy -= 4 * dt;
                p.rot += p.spin * dt;
                const u = p.age / p.life;
                if (p.kind === "smoke") {
                    x.save();
                    x.globalCompositeOperation = "screen";
                    smoke(p.x, p.y, p.size + p.grow * u, p.alpha * Math.sin(Math.PI * u));
                    x.restore();
                } else if (p.kind === "star") {
                    star(p.x, p.y, p.size * Math.sin(Math.PI * u), p.rot, 1);
                    if (!p.noDust && Math.random() < .25) add({ kind: "dust", x: p.x, y: p.y, vx: (Math.random() - .5) * 14, vy: 6 + Math.random() * 10,
                        size: .4 + Math.random() * .7, life: .7 + Math.random() * .6, alpha: .85, colour: randomColour() });
                } else if (p.kind === "glint") {
                    x.save();
                    x.globalCompositeOperation = "lighter";
                    const a = Math.sin(Math.PI * u) * Math.max(0, Math.sin(p.age * 9 + p.phase));
                    glow(x, p.x, p.y, p.size * 3, a * .8, p.colour);
                    x.fillStyle = `rgba(245,255,252,${a})`;
                    x.beginPath();
                    x.arc(p.x, p.y, p.size * .55, 0, 6.283);
                    x.fill();
                    x.restore();
                } else {
                    x.save();
                    x.globalCompositeOperation = "lighter";
                    glow(x, p.x, p.y, p.size * 2.8, p.alpha * (1 - u * u) * (.55 + .45 * Math.sin(p.age * 16 + p.phase)), p.colour);
                    x.restore();
                }
            }

            if (t > DONE && !leavingNow) {
                leavingNow = true;
                setLeaving(true);
            }
            frameId = requestAnimationFrame(draw);
        };

        // Start when the font and the star are ready (the dark sky and the aurora are already showing)
        let cancelled = false;
        Promise.all([document.fonts.load(`700 100px ${font}`).catch(() => undefined), starImage.decode().catch(() => undefined)]).then(() => {
            if (cancelled) return;
            layout();
            frameId = requestAnimationFrame(draw);
        });

        return () => {
            cancelled = true;
            cancelAnimationFrame(frameId);
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

    return (
        <div className={leaving ? `${styles.intro} ${styles.leaving}` : styles.intro}>
            <div className={styles.aurora} aria-hidden="true"><i className={styles.a1}/><i className={styles.a2}/><i className={styles.a3}/></div>
            <canvas ref={canvasRef} className={styles.canvas} aria-hidden="true"/>
            <button type="button" className={styles.skip} onClick={() => setLeaving(true)} aria-label="Hopp over introen"/>
        </div>
    );
}
