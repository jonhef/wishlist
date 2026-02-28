var _a, _b, _c, _d, _e;
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
var backendUrl = (_a = process.env.BACKEND_URL) !== null && _a !== void 0 ? _a : "http://localhost:8080";
var allowedHosts = ["wishlist.jonhef.org"];
export default defineConfig({
    plugins: [react()],
    test: {
        environment: "node",
        include: ["src/**/*.spec.ts"]
    },
    server: {
        host: (_b = process.env.HOST) !== null && _b !== void 0 ? _b : "0.0.0.0",
        port: Number((_c = process.env.PORT) !== null && _c !== void 0 ? _c : 5173),
        allowedHosts: allowedHosts,
        proxy: {
            "/api": {
                target: backendUrl,
                changeOrigin: true,
                rewrite: function (path) { return path.replace(/^\/api/, ""); }
            }
        }
    },
    preview: {
        host: (_d = process.env.HOST) !== null && _d !== void 0 ? _d : "0.0.0.0",
        port: Number((_e = process.env.PORT) !== null && _e !== void 0 ? _e : 5173),
        allowedHosts: allowedHosts
    }
});
