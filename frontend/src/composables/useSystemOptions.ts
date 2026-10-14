import { onMounted, ref } from 'vue'
import { api } from '../services/api'
import type { SystemOptions } from '../types'

const emptyOptions: SystemOptions = {
  projectTypes: [],
  structureTypes: [],
  cities: [],
  materials: [],
}

let cached: SystemOptions | null = null

export function useSystemOptions() {
  const options = ref<SystemOptions>(cached ?? emptyOptions)
  const loading = ref(!cached)
  const error = ref('')

  onMounted(async () => {
    if (cached) return
    try {
      cached = await api<SystemOptions>('/system/options')
      options.value = cached
    } catch (caught) {
      error.value = caught instanceof Error ? caught.message : '無法載入選項'
    } finally {
      loading.value = false
    }
  })

  return { options, loading, error }
}
