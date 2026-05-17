<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = defineProps<{
  modelValue: string
  countdown: number
  error?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
  submit: []
  back: []
}>()

const digits = ref<string[]>(['', '', '', '', '', ''])
const inputs = ref<HTMLInputElement[]>([])
const inputRef = (el: Element | null, i: number) => {
  if (el) inputs.value[i] = el as HTMLInputElement
}

watch(() => props.modelValue, (val) => {
  if (val.length === 0) {
    digits.value = ['', '', '', '', '', '']
  }
})

function handleInput(i: number, e: Event) {
  const target = e.target as HTMLInputElement
  const val = target.value.replace(/\D/g, '')
  if (val.length > 1) {
    target.value = val[val.length - 1]
  }
  digits.value[i] = target.value

  if (target.value && i < 5) {
    inputs.value[i + 1]?.focus()
  }

  const code = digits.value.join('')
  emit('update:modelValue', code)

  if (code.length === 6) {
    emit('submit')
  }
}

function handleKeydown(i: number, e: KeyboardEvent) {
  if (e.key === 'Backspace' && !digits.value[i] && i > 0) {
    inputs.value[i - 1]?.focus()
  }
}

function handlePaste(e: ClipboardEvent) {
  e.preventDefault()
  const text = e.clipboardData?.getData('text')?.replace(/\D/g, '').slice(0, 6) || ''
  for (let i = 0; i < 6; i++) {
    digits.value[i] = text[i] || ''
    if (inputs.value[i]) inputs.value[i].value = text[i] || ''
  }
  emit('update:modelValue', digits.value.join(''))
  if (text.length === 6) {
    nextTick(() => emit('submit'))
  }
}
</script>

<template>
  <div class="totp-input-group">
    <input
      v-for="(_, i) in 6"
      :key="i"
      :ref="(el) => inputRef(el as Element | null, i)"
      v-model="digits[i]"
      type="text"
      inputmode="numeric"
      maxlength="1"
      class="totp-digit"
      :class="{ filled: digits[i] !== '' }"
      data-test="mfa-code-input"
      @input="handleInput(i, $event)"
      @keydown="handleKeydown(i, $event)"
      @paste="i === 0 && handlePaste($event)"
    />
  </div>
</template>

<style scoped>
.totp-input-group {
  display: flex;
  gap: 10px;
  justify-content: center;
}

.totp-digit {
  width: 48px;
  height: 56px;
  text-align: center;
  font-size: 24px;
  font-weight: 600;
  font-family: "SF Mono", Menlo, Monaco, Consolas, monospace;
  border: 2px solid var(--color-border-2);
  border-radius: 8px;
  outline: none;
  background: var(--color-bg-1);
  color: var(--color-text-1);
  transition: all 0.15s ease;
}

.totp-digit:focus {
  border-color: rgb(var(--primary-6));
  box-shadow: 0 0 0 3px rgba(var(--primary-6), 0.15);
}

.totp-digit.filled {
  border-color: var(--color-border-3);
  background: var(--color-bg-2);
}

@media (max-width: 480px) {
  .totp-input-group {
    gap: 6px;
  }

  .totp-digit {
    width: 42px;
    height: 50px;
    font-size: 20px;
  }
}
</style>
