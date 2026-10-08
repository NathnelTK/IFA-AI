/** @type {import('tailwindcss').Config} */
import typography from '@tailwindcss/typography';

export default {
  content: ['./src/**/*.{html,js,svelte,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        ifa: {
          bg: '#F8F6F0',
          'bg-warm': '#F3EFE6',
          sidebar: '#F2EFEB',
          card: '#FFFFFF',
          'card-muted': '#F7F5F0',
          border: '#E8E3DA',
          'border-light': '#F0ECE4',
          pine: {
            DEFAULT: '#1B3D2F',
            dark: '#132C22',
            light: '#285844',
            hover: '#224B3A'
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
            primary: '#1F2923',
            secondary: '#606C64',
            muted: '#8B978F'
          }
        }
      },
      fontFamily: {
        sans: ['Plus Jakarta Sans', 'Inter', 'system-ui', 'sans-serif']
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