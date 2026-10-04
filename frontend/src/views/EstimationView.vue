<script setup lang="ts">
import { computed, ref } from 'vue'
import DataTable from '../components/DataTable.vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api } from '../services/api'
import type { JsonRecord, ProjectForm } from '../types'

const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({
  projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null,
  floorCount: null, basementFloorCount: 0, projectMonth: '',
})
const selectedMaterials = ref<number[]>([])
const mode = ref<'core' | 'selected'>('core')
const loading = ref(false)
const error = ref('')
const result = ref<JsonRecord | null>(null)

const rows = computed<JsonRecord[]>(() => {
  if (!result.value) return []
  const candidate = result.value.materials ?? result.value.items ?? result.value.results
  return Array.isArray(candidate) ? candidate as JsonRecord[] : []
})

const target = computed(() => ({
  targetMode: 1,
  projectType: project.value.projectType,
  structureType: project.value.structureType,
  city: project.value.city,
  totalFloorArea: project.value.totalFloorArea,
  floorCount: project.value.floorCount,
  basementFloorCount: project.value.basementFloorCount,
  projectMonth: project.value.projectMonth,
}))

async function estimate() {
  loading.value = true
  error.value = ''
  result.value = null
  try {
    result.value = mode.value === 'core'
      ? await api<JsonRecord>('/range-estimation/core', { method: 'POST', body: JSON.stringify(target.value) })
      : await api<JsonRecord>('/range-estimation/batch', {
          method: 'POST',
          body: JSON.stringify({ target: target.value, materialIds: selectedMaterials.value }),
        })
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '估算失敗'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div><h2>材料數量區間估算</h2><p>輸入新建案條件，估算核心材料或自選材料的合理數量範圍。</p></div>
      <div class="tabs">
        <button type="button" class="tab" :class="{ active: mode === 'core' }" @click="mode = 'core'">核心材料</button>
        <button type="button" class="tab" :class="{ active: mode === 'selected' }" @click="mode = 'selected'">自選材料</button>
      </div>
    </div>
    <div v-if="optionsError" class="notice error">{{ optionsError }}</div>
    <div v-if="error" class="notice error">{{ error }}</div>

    <form class="card" @submit.prevent="estimate">
      <div class="card-header"><h3>目標建案條件</h3></div>
      <div class="card-body">
        <ProjectFields v-model="project" :options="options" />
        <label v-if="mode === 'selected'" class="field" style="margin-top: 18px">
          <span>選擇材料（可複選）</span>
          <select v-model="selectedMaterials" multiple size="8" required>
            <option v-for="item in options.materials" :key="item.materialId" :value="item.materialId">
              {{ item.category }}｜{{ item.materialName }}（{{ item.unitName }}）
            </option>
          </select>
        </label>
        <div class="actions">
          <button class="button primary" type="submit" :disabled="loading || (mode === 'selected' && !selectedMaterials.length)">
            {{ loading ? '估算中…' : '執行估算' }}
          </button>
        </div>
      </div>
    </form>

    <section v-if="result" class="card">
      <div class="card-header"><h3>估算結果</h3></div>
      <DataTable :rows="rows" />
      <div v-if="!rows.length" class="card-body"><pre class="json-result">{{ JSON.stringify(result, null, 2) }}</pre></div>
    </section>
  </div>
</template>
