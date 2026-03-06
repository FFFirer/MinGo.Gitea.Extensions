/** @type {import('tailwindcss').Config} */
import daisyui from "daisyui"

export default {
  content: [
    "./BlazorApp/**/*.{razor,html,cshtml}",
    "./BlazorApp/**/*.{js,ts,jsx,tsx}",
    "./BlazorApp.Client/**/*.{razor,html,cshtml}",
    "./BlazorApp.Client/**/*.{js,ts,jsx,tsx}"
  ],
  plugins: [
    daisyui
  ]
}