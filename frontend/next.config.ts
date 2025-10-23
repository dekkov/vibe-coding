import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  compress: true,
  reactStrictMode: false, // Disable strict mode to prevent double-mounting issues with SignalR
};

export default nextConfig;
