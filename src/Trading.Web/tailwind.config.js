/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        action: {
          buy: "#10b981",
          sell: "#ef4444",
          hold: "#64748b",
        },
      },
    },
  },
  plugins: [],
};
