import type {NextConfig} from "next";

const nextConfig: NextConfig = {
    // Do not tell visitors which framework we use
    poweredByHeader: false,
    // The Docker image only gets the files the server needs (see web/Dockerfile)
    output: "standalone",
    allowedDevOrigins: ["192.168.1.203"],
    experimental: {
        // Room for one shrunk photo; stays under Vercel's 4.5 MB request limit
        serverActions: {bodySizeLimit: "4mb"},

    },
    // The customer page moved from /mine-bestillinger to /min-besti
    async redirects() {
        return [
            {source: "/mine-bestillinger", destination: "/min-besti", permanent: true},
            {source: "/mine-bestillinger/:path*", destination: "/min-besti/:path*", permanent: true},
        ];
    },
    // Security headers on every page
    async headers() {
        return [
            {
                source: "/:path*",
                headers: [
                    // Nobody may show BESTI inside their own page (stops fake "Bestill" buttons on top of ours)
                    {key: "X-Frame-Options", value: "DENY"},
                    // The browser must trust our file types instead of guessing
                    {key: "X-Content-Type-Options", value: "nosniff"},
                    // Other sites only see "bestiapp.no", never the full address with booking details
                    {key: "Referrer-Policy", value: "strict-origin-when-cross-origin"},
                    // No camera or microphone; location only for our own "Nær meg"
                    {key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(self)"},
                ],
            },
        ];
    },
};

export default nextConfig;
