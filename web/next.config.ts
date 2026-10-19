import type {NextConfig} from "next";

const nextConfig: NextConfig = {
    /* config options here */
    experimental: {
    // Room for one shrunk photo; stays under Vercel's 4.5 MB request limit
        serverActions: {bodySizeLimit: "4mb"},

    },
};

export default nextConfig;
