<script setup lang="ts">
import { computed } from 'vue'
import type { JsonRecord } from '../types'

const props = withDefaults(defineProps<{
  rows: JsonRecord[]
  emptyText?: string
  maxColumns?: number
}>(), {
  emptyText: '目前沒有資料',
  maxColumns: 8,
})

const columns = computed(() => {
  const names = new Set<string>()
  props.rows.slice(0, 20).forEach((row) => Object.keys(row).forEach((key) => names.add(key)))
  return Array.from(names).slice(0, props.maxColumns)
})

function display(value: unknown): string {
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'boolean') return value ? '是' : '否'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}
</script>

<template>
  <div v-if="rows.length" class="table-wrap">
    <table>
      <thead>
        <tr><th v-for="column in columns" :key="column">{{ column }}</th></tr>
      </thead>
      <tbody>
        <tr v-for="(row, index) in rows" :key="index">
          <td v-for="column in columns" :key="column" :title="display(row[column])">
            {{ display(row[column]) }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  <div v-else class="empty-state">{{ emptyText }}</div>
</template>
