import { createI18n } from 'vue-i18n'
import zhCN from './locales/zh-CN.json'
import enUS from './locales/en-US.json'

const DEFAULT_LOCALE = 'zh-CN'

function loadLocale(): string {
  const saved = localStorage.getItem('locale')
  if (saved && ['zh-CN', 'en-US'].includes(saved)) return saved
  const navLang = navigator.language
  if (navLang.startsWith('zh')) return 'zh-CN'
  return DEFAULT_LOCALE
}

export type LocaleType = 'zh-CN' | 'en-US'

export const supportedLocales: { key: LocaleType; label: string }[] = [
  { key: 'zh-CN', label: '简体中文' },
  { key: 'en-US', label: 'English' },
]

export const i18n = createI18n({
  legacy: false,
  locale: loadLocale(),
  fallbackLocale: 'en-US',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
  },
})
