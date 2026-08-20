import { createI18n } from 'vue-i18n'
import zhCN from './locales/zh-CN.json'
import enUS from './locales/en-US.json'
import eo from './locales/eo.json'
import vi from './locales/vi.json'

const DEFAULT_LOCALE = 'zh-CN'

function loadLocale(): string {
  const saved = localStorage.getItem('locale')
  if (saved && ['zh-CN', 'en-US', 'eo', 'vi'].includes(saved)) return saved
  const navLang = navigator.language
  if (navLang.startsWith('zh')) return 'zh-CN'
  if (navLang.startsWith('vi')) return 'vi'
  return DEFAULT_LOCALE
}

export type LocaleType = 'zh-CN' | 'en-US' | 'eo' | 'vi'

export const supportedLocales: { key: LocaleType; label: string }[] = [
  { key: 'zh-CN', label: '简体中文' },
  { key: 'en-US', label: 'English' },
  { key: 'eo', label: 'Esperanto' },
  { key: 'vi', label: 'Tiếng Việt' },
]

export const i18n = createI18n({
  legacy: false,
  locale: loadLocale(),
  fallbackLocale: 'en-US',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS,
    eo,
    vi,
  },
})
