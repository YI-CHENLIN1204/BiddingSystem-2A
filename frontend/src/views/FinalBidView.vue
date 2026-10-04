<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from '../components/DataTable.vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api, apiForm, queryString } from '../services/api'
import type { JsonRecord, ProjectForm } from '../types'

const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({
  projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null,
  floorCount: null, basementFloorCount: 0, projectMonth: '',
})
const file = ref<File | null>(null)
const loading = ref(false)
const error = ref('')
const message = ref('')
const importResult = ref<JsonRecord | null>(null)
const mappingResult = ref<JsonRecord | null>(null)
const projectId = ref<number | null>(null)
const status = ref('Pending')
const search = ref('')

const mappingRows = computed<JsonRecord[]>(() => {
  const value = mappingResult.value
  if (!value) return []
  const candidate = value.items ?? value.rows ?? value.data
  return Array.isArray(candidate) ? candidate as JsonRecord[] : []
})

function selectFile(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

async function upload() {
  if (!file.value) return
  loading.value = true
  error.value = ''
  message.value = ''
  const form = new FormData()
  form.append('excelFile', file.value)
  Object.entries(project.value).forEach(([key, value]) => {
    if (value !== null) form.append(key, String(value))
  })

  try {
    importResult.value = await apiForm<JsonRecord>('/Estimation/upload-final-bid', form)
    const id = importResult.value.projectId ?? importResult.value.projectMasterId
    projectId.value = typeof id === 'number' ? id : Number(id) || null
    message.value = '決標資料匯入完成。'
    await loadMappings()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '匯入失敗'
  } finally {
    loading.value = false
  }
}

async function loadMappings() {
  error.value = ''
  try {
    mappingResult.value = await api<JsonRecord>(`/Estimation/mapping-items${queryString({
      projectId: projectId.value,
      status: status.value,
      search: search.value,
      pageSize: 100,
    })}`)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入材料對應資料'
  }
}
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div><h2>決標資料匯入</h2><p>建立歷史專案並匯入決標 Excel，匯入後可立即檢查材料對應狀態。</p></div>
    </div>
    <div v-if="optionsError" class="notice error">{{ optionsError }}</div>
    <div v-if="error" class="notice error">{{ error }}</div>
    <div v-if="message" class="notice success">{{ message }}</div>

    <form class="card" @submit.prevent="upload">
      <div class="card-header"><h3>專案與檔案</h3></div>
      <div class="card-body">
        <ProjectFields v-model="project" :options="options" include-name />
        <div class="file-drop" style="margin-top: 18px">
          <label class="field"><span>決標 Excel</span><input type="file" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" required @change="selectFile" /></label>
        </div>
        <div class="actions"><button class="button primary" type="submit" :disabled="loading">{{ loading ? '匯入中…' : '匯入決標資料' }}</button></div>
      </div>
    </form>

    <section v-if="importResult" class="card">
      <div class="card-header"><h3>匯入結果</h3></div>
      <div class="card-body"><pre class="json-result">{{ JSON.stringify(importResult, null, 2) }}</pre></div>
    </section>

    <section class="card">
      <div class="card-header"><h3>材料對應檢查</h3></div>
      <div class="card-body toolbar">
        <label class="field"><span>專案 ID</span><input v-model.number="projectId" type="number" min="1" /></label>
        <label class="field"><span>狀態</span><select v-model="status"><option>Pending</option><option>Mapped</option><option>Ignored</option><option>All</option></select></label>
        <label class="field"><span>搜尋</span><input v-model="search" placeholder="項目或材料名稱" /></label>
        <button class="button secondary" type="button" @click="loadMappings">查詢</button>
      </div>
      <DataTable :rows="mappingRows" empty-text="輸入專案 ID 後查詢材料對應狀態" />
    </section>
  </div>
</template>
