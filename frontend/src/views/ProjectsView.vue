<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, queryString } from '../services/api'
import type { ProjectSummary } from '../types'

const projects = ref<ProjectSummary[]>([])
const search = ref('')
const loading = ref(false)
const error = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try {
    const response = await api<{ items: ProjectSummary[] }>(`/projects${queryString({ search: search.value })}`)
    projects.value = response.items
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入專案'
  } finally {
    loading.value = false
  }
}

async function remove(project: ProjectSummary) {
  if (!window.confirm(`確定要刪除「${project.projectName}」？`)) return
  try {
    await api(`/projects/${project.projectId}`, { method: 'DELETE' })
    await load()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '刪除失敗'
  }
}

onMounted(load)
</script>

<template>
  <div class="page-stack">
    <div class="page-intro"><div><h2>專案管理</h2><p>查詢歷史建案、決標項目與歷史交易資料。</p></div></div>
    <div v-if="error" class="notice error">{{ error }}</div>
    <section class="card">
      <div class="card-body toolbar">
        <label class="field"><span>關鍵字</span><input v-model="search" placeholder="專案名稱、類型或縣市" @keyup.enter="load" /></label>
        <button class="button primary" type="button" :disabled="loading" @click="load">{{ loading ? '查詢中…' : '查詢' }}</button>
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>專案名稱</th><th>類型</th><th>結構</th><th>縣市</th><th>總樓地板面積</th><th>決標項目</th><th></th></tr></thead>
          <tbody>
            <tr v-for="item in projects" :key="item.projectId">
              <td>{{ item.projectName }}</td><td>{{ item.projectType || '—' }}</td><td>{{ item.structureType || '—' }}</td><td>{{ item.city || '—' }}</td>
              <td>{{ item.totalFloorArea ?? '—' }}</td><td>{{ item.finalBidItemCount ?? 0 }}</td>
              <td><div class="inline-actions"><RouterLink class="button secondary small" :to="`/projects/${item.projectId}`">明細</RouterLink><button class="button danger small" type="button" @click="remove(item)">刪除</button></div></td>
            </tr>
          </tbody>
        </table>
        <div v-if="!projects.length && !loading" class="empty-state">目前沒有符合條件的專案</div>
      </div>
    </section>
  </div>
</template>
