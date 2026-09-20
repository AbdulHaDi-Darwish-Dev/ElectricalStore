import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    // Provider-neutral remote allowlist for current public media hosts.
    // Do not encode Cloudinary transforms or public IDs here.
    remotePatterns: [
      {
        protocol: "https",
        hostname: "res.cloudinary.com",
        pathname: "/**",
      },
    ],
  },
};

export default nextConfig;
