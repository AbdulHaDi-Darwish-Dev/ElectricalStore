// Next.js standalone output for Docker (see frontend/Dockerfile).
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    // Provider-neutral remote allowlist for current public media hosts.
    // Do not encode Cloudinary transforms or public IDs here.
    // Next.js 16 blocks private IPs (localhost API media) unless explicitly allowed.
    // Keep Development-only so Production never optimizes internal network URLs.
    dangerouslyAllowLocalIP: process.env.NODE_ENV === "development",
    remotePatterns: [
      {
        protocol: "https",
        hostname: "res.cloudinary.com",
        pathname: "/**",
      },
      {
        protocol: "https",
        hostname: "picsum.photos",
        pathname: "/**",
      },
      // Legacy local fixture host (migrated to picsum); keep allowlist if any rows remain.
      {
        protocol: "https",
        hostname: "img.test",
        pathname: "/**",
      },
      // Development demo-catalog static files served by the API.
      {
        protocol: "http",
        hostname: "localhost",
        port: "5180",
        pathname: "/demo-catalog/**",
      },
      {
        protocol: "http",
        hostname: "127.0.0.1",
        port: "5180",
        pathname: "/demo-catalog/**",
      },
    ],
  },
};

export default nextConfig;
