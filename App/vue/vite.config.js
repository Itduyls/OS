import { fileURLToPath, URL } from "url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [vue(), ],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src",
                import.meta.url)),
        },
    },
    define: {
        baseURL: JSON.stringify("http://192.168.1.90:7777"),
        SecretKey: JSON.stringify("1012198815021989"),
    },
    server: {
        host: true
    },

});