<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from '../components/DataTable.vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { apiForm } from '../services/api'
import type { JsonRecord, ProjectForm } from '../types'

const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({
  projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null,
  floorCount: null, basementFloorCount: 0, projectMonth: '',
})
const file = ref<File | null>(null)
const loading = ref(false)
const error = ref('')
const result = ref<JsonRecord | null>(null)

const resultRows = computed<JsonRecord[]>(() => {
  if (!result.value) return []
  const candidate = result.value.materialResults ?? result.value.items ?? result.value.results
  return Array.isArray(candidate) ? candidate as JsonRecord[] : []
})

function selectFile(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

async function analyze() {
  if (!file.value) {
    error.value = '請先選擇標單檔案。'
    return
  }

  loading.value = true
  error.value = ''
  result.value = null
  const form = new FormData()
  form.append('excelFile', file.value)
  Object.entries(project.value).forEach(([key, value]) => {
    if (key !== 'projectName' && value !== null) form.append(key, String(value))
  })

  try {
    result.value = await apiForm<JsonRecord>('/bid-anomaly/analyze', form)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '分析失敗'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div>
        <h2>標單數量異常偵測</h2>
        <p>上傳工程標單，以歷史相似專案的數量區間找出偏高、偏低及資料不足項目。</p>
      </div>
    </div>

    <div v-if="optionsError" class="notice error">{{ optionsError }}</div>
    <div v-if="error" class="notice error">{{ error }}</div>

    <form class="card" @submit.prevent="analyze">
      <div class="card-header"><h3>分析條件</h3></div>
      <div class="card-body">
        <ProjectFields v-model="project" :options="options" />
        <div class="file-drop" style="margin-top: 18px">
          <label class="field">
            <span>Excel 或 CSV 標單</span>
            <input type="file" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" required @change="selectFile" />
          </label>
        </div>
        <div class="actions">
          <button class="button primary" type="submit" :disabled="loading">
            {{ loading ? '分析中…' : '開始分析' }}
          </button>
        </div>
      </div>
    </form>

    <template v-if="result">
      <div class="stat-grid">
        <div class="stat"><span>解析筆數</span><strong>{{ result.parsedRowCount ?? '—' }}</strong></div>
        <div class="stat"><span>已對應</span><strong>{{ result.mappedRowCount ?? '—' }}</strong></div>
        <div class="stat"><span>高於區間</span><strong>{{ result.highCount ?? '—' }}</strong></div>
        <div class="stat"><span>低於區間</span><strong>{{ result.lowCount ?? '—' }}</strong></div>
      </div>
      <section class="card">
        <div class="card-header"><h3>材料分析結果</h3></div>
        <DataTable :rows="resultRows" />
      </section>
      <details class="card">
        <summary class="card-header">檢視完整回應</summary>
        <div class="card-body"><pre class="json-result">{{ JSON.stringify(result, null, 2) }}</pre></div>
      </details>
    </template>
  </div>
</template>
