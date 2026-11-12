/** @type {import('tailwindcss').Config} */
import typography from '@tailwindcss/typography';

export default {
  content: ['./src/**/*.{html,js,svelte,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        ifa: {
          bg: '#F2EFE7',
          'bg-warm': '#EAE5D9',
          sidebar: '#EDE8DD',
          card: '#FBFAF6',
          'card-muted': '#EDE8DD',
          border: '#CFC8B9',
          'border-light': '#DED8CB',
          pine: {
            DEFAULT: '#17221C',
            dark: '#0E1511',
            light: '#294437',
            hover: '#24362D'
          },
          accent: {
            green: '#2A9D68',
            orange: '#E07A5F',
            amber: '#D97706',
            purple: '#7C5CFC',
            blue: '#2563EB',
            pink: '#E11D48'
          },
          text: {
            primary: '#17221C',
            secondary: '#4C574F',
            muted: '#657068'
          }
        }
      },
      fontFamily: {
        sans: ['Manrope Variable', 'Manrope', 'system-ui', 'sans-serif'],
        display: ['Newsreader Variable', 'Newsreader', 'Georgia', 'serif']
      },
      boxShadow: {
        'soft': '0 2px 10px rgba(0, 0, 0, 0.03)',
        'card': '0 4px 20px -2px rgba(27, 61, 47, 0.05)',
        'elevated': '0 10px 30px -5px rgba(27, 61, 47, 0.08)'
      },
      borderRadius: {
        '2xl': '1rem',
        '3xl': '1.5rem',
        '4xl': '2rem'
      }
    }
  },
  plugins: [typography]
};
