/* Pre-paint theme bootstrap (T1/T5): sets data-bs-theme before first paint to
 * avoid a light-mode flash. Loaded in <head> by design (X1 exception).
 * Wrapped in an IIFE so an accidental double include cannot throw a global
 * const-redeclaration SyntaxError (JS4). */
(function () {
    'use strict';

    const storedTheme = localStorage.getItem('theme')

    const getPreferredTheme = () => {
        if (storedTheme) {
            return storedTheme
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
    }

    const setTheme = function (theme) {
        if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-bs-theme', 'dark')
        } else {
            document.documentElement.setAttribute('data-bs-theme', theme)
        }
    }

    setTheme(getPreferredTheme())

    window.addEventListener('DOMContentLoaded', () => {
        var el = document.querySelector('.theme-icon-active');
        if (el !== undefined) {
            const showActiveTheme = theme => {
                document.querySelectorAll('[data-bs-theme-value]').forEach(element => {
                    element.classList.remove('active')
                })

                const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`)
                if (btnToActive) {
                    const svgOfActiveBtn = btnToActive.querySelector('svg use').getAttribute('href')

                    btnToActive.classList.add('active')
                    document.querySelector('.theme-icon-active use').setAttribute('href', svgOfActiveBtn)
                }
            }

            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
                // Follow OS preference only when the user has not pinned a theme.
                if (storedTheme !== 'light' && storedTheme !== 'dark') {
                    setTheme(getPreferredTheme())
                }
            })

            showActiveTheme(getPreferredTheme())

            document.querySelectorAll('[data-bs-theme-value]')
                .forEach(toggle => {
                    toggle.addEventListener('click', () => {
                        const theme = toggle.getAttribute('data-bs-theme-value')
                        localStorage.setItem('theme', theme)
                        setTheme(theme)
                        showActiveTheme(theme)
                    })
                })
        }
    })
})()
