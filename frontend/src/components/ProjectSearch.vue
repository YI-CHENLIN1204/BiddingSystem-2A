<script setup lang="ts">
import { computed } from 'vue'
import { emptyProjectSearch, type ProjectSearchFilters, type SearchableProject } from '../services/projectSearch'

const props = defineProps<{
  projects: SearchableProject[]
  stageLabel: string
  stages: { value: string; label: string }[]
}>()
const filters = defineModel<ProjectSearchFilters>({ required: true })
const columns = [
  { key: 'projectType', label: '建案類型' },
  { key: 'structureType', label: '結構類型' },
  { key: 'city', label: '縣市' },
] as const
const selectFields = computed(() => columns.map(column => ({
  ...column,
  options: [...new Set(props.projects.flatMap(project => project[column.key] || []))]
    .sort((a, b) => a.localeCompare(b, 'zh-TW')),
})))

function setFilter(key: keyof ProjectSearchFilters, event: Event) {
  const input = event.target as HTMLInputElement | HTMLSelectElement
  filters.value = { ...filters.value, [key]: input.value }
}
</script>

<template>
  <section class="card p-3 mb-3" aria-label="搜尋條件">
    <div class="row g-2 align-items-end">
      <label class="col-12 col-lg">
        <span class="form-label">關鍵字</span>
        <input class="form-control" type="search" placeholder="名稱或縣市" :value="filters.keyword" @input="setFilter('keyword', $event)" />
      </label>
      <label v-for="field in selectFields" :key="field.key" class="col-6 col-md">
        <span class="form-label">{{ field.label }}</span>
        <select class="form-select" :value="filters[field.key]" @change="setFilter(field.key, $event)">
          <option value="">全部</option>
          <option v-for="option in field.options" :key="option" :value="option">{{ option }}</option>
        </select>
      </label>
      <label class="col-6 col-md">
        <span class="form-label">{{ stageLabel }}</span>
        <select class="form-select" :value="filters.stage" @change="setFilter('stage', $event)">
          <option value="">全部</option>
          <option v-for="stage in stages" :key="stage.value" :value="stage.value">{{ stage.label }}</option>
        </select>
      </label>
      <div class="col-auto">
        <button type="button" class="btn btn-outline-secondary" @click="filters = emptyProjectSearch()">清除</button>
      </div>
    </div>
  </section>
</template>
