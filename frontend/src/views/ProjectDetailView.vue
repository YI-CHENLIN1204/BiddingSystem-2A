<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import DataTable from '../components/DataTable.vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api, queryString } from '../services/api'
import type { JsonRecord, ProjectForm } from '../types'

const route = useRoute()
const projectId = Number(route.params.id)
const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({ projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null, floorCount: null, basementFloorCount: 0, projectMonth: '' })
const itemsResult = ref<JsonRecord | null>(null)
const historyResult = ref<unknown>(null)
const tab = ref<'items' | 'history'>('items')
const search = ref('')
const error = ref('')
const message = ref('')
const loading = ref(false)

const itemRows = computed<JsonRecord[]>(() => {
  const value = itemsResult.value
  if (!value) return []
  const candidate = value.items ?? value.rows ?? value.data
  return Array.isArray(candidate) ? candidate as JsonRecord[] : []
})
const historyRows = computed<JsonRecord[]>(() => Array.isArray(historyResult.value) ? historyResult.value as JsonRecord[] : [])

async function loadProject() {
  const data = await api<JsonRecord>(`/projects/${projectId}`)
  const startDate = String(data.startDate ?? data.projectMonth ?? '')
  project.value = {
    projectName: String(data.projectName ?? ''),
    projectType: String(data.projectType ?? ''),
    structureType: String(data.structureType ?? ''),
    city: String(data.city ?? ''),
    totalFloorArea: data.totalFloorArea == null ? null : Number(data.totalFloorArea),
    floorCount: data.floorCount == null ? null : Number(data.floorCount),
    basementFloorCount: data.basementFloorCount == null ? 0 : Number(data.basementFloorCount),
    projectMonth: startDate.slice(0, 7),
  }
}

async function loadItems() {
  itemsResult.value = await api<JsonRecord>(`/projects/${projectId}/final-bid-items${queryString({ search: search.value, pageSize: 100 })}`)
}

async function loadHistory() {
  historyResult.value = await api<unknown>(`/projects/${projectId}/historical-data`)
}

async function save() {
  loading.value = true
  error.value = ''
  message.value = ''
  try {
    await api(`/projects/${projectId}`, { method: 'PUT', body: JSON.stringify(project.value) })
    message.value = '專案資料已更新。'
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '更新失敗'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    await Promise.all([loadProject(), loadItems(), loadHistory()])
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入專案資料'
  }
})
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div><h2>{{ project.projectName || `專案 #${projectId}` }}</h2><p>編輯專案條件，檢視原始決標項目及轉入的歷史資料。</p></div>
      <RouterLink class="button secondary" to="/projects">返回專案列表</RouterLink>
    </div>
    <div v-if="optionsError || error" class="notice error">{{ optionsError || error }}</div>
    <div v-if="message" class="notice success">{{ message }}</div>

    <form class="card" @submit.prevent="save">
      <div class="card-header"><h3>專案資料</h3></div>
      <div class="card-body">
        <ProjectFields v-model="project" :options="options" include-name />
        <div class="actions"><button class="button primary" type="submit" :disabled="loading">{{ loading ? '儲存中…' : '儲存變更' }}</button></div>
      </div>
    </form>

    <div class="tabs">
      <button class="tab" :class="{ active: tab === 'items' }" @click="tab = 'items'">決標項目</button>
      <button class="tab" :class="{ active: tab === 'history' }" @click="tab = 'history'">歷史資料</button>
    </div>
    <section v-if="tab === 'items'" class="card">
      <div class="card-body toolbar"><label class="field"><span>搜尋決標項目</span><input v-model="search" @keyup.enter="loadItems" /></label><button class="button secondary" @click="loadItems">查詢</button></div>
      <DataTable :rows="itemRows" />
    </section>
    <section v-else class="card">
      <DataTable :rows="historyRows" />
      <div v-if="!historyRows.length && historyResult" class="card-body"><pre class="json-result">{{ JSON.stringify(historyResult, null, 2) }}</pre></div>
    </section>
  </div>
</template>
