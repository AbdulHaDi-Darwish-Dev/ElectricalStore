// Next.js standalone output for Docker (see frontend/Dockerfile).
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    // Provider-neutral remote allowlist for current public media hosts.
    // Do not encode Cloudinary transforms or public IDs here.
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
    ],
  },
};

export default nextConfig;
