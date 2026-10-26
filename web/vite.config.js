import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Dev server proxies the API endpoint to the running .NET service so the
// browser calls a same-origin path in development too. The production build is
// served as static files by the API itself (same origin, no proxy needed).
export default defineConfig({
    plugins: [react()],
    server: {
        proxy: {
            "/kyc-data": "http://localhost:5034",
        },
    },
});
