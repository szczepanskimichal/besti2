// Where to go after login: only a page on our own site.
// The browser reads "//evil.com", "/\evil.com" and "/<tab>/evil.com" as another site,
// so we let the URL parser decide instead of checking the text by hand.
export function safeRedirectPath(next: string): string {
    if (!next.startsWith("/")) return "/";
    try {
        const url = new URL(next, "http://besti.local");
        return url.origin === "http://besti.local" ? `${url.pathname}${url.search}${url.hash}` : "/";
    } catch {
        return "/";
    }
}