import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

const backendUrl = process.env.BACKEND_URL ?? "http://localhost:8080";
const allowedHosts = ["wishlist.jonhef.org"];

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "node",
    include: ["src/**/*.spec.ts"]
  },
  server: {
    host: process.env.HOST ?? "0.0.0.0",
    port: Number(process.env.PORT ?? 5173),
    allowedHosts,
    proxy: {
      "/api": {
        target: backendUrl,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, "")
      }
    }
  },
  preview: {
    host: process.env.HOST ?? "0.0.0.0",
    port: Number(process.env.PORT ?? 5173),
    allowedHosts
  }
});
