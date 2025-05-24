/** @type {import('tailwindcss').Config} */
import { heroui } from "@heroui/react";

module.exports = {
  content: ["./app/**/*.{html, js, jsx, ts, tsx}",
     "./node_modules/@heroui/theme/dist/**/*.{js,ts,jsx,tsx}"
  ],
  theme: {
    extend: {},
  },
  darkMode: "class",
  plugins: [heroui(),
  ],
}
