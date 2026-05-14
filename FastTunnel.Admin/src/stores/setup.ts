import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getSetupStatus } from '@/api/setup'

export const useSetupStore = defineStore('setup', () => {
  const initialized = ref<boolean | null>(null)
  const loading = ref(false)
  const error = ref(false)

  const needsSetup = computed(() => initialized.value === false)

  async function checkStatus() {
    loading.value = true
    error.value = false
    try {
      const data = await getSetupStatus()
      initialized.value = data.initialized
    } catch {
      error.value = true
    } finally {
      loading.value = false
    }
  }

  function markInitialized() {
    initialized.value = true
    error.value = false
  }

  return { initialized, loading, error, needsSetup, checkStatus, markInitialized }
})
