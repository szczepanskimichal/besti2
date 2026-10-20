import type {NextConfig} from "next";

const nextConfig: NextConfig = {
    /* config options here */
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
};

export default nextConfig;
