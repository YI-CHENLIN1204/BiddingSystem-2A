<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, queryString } from '../services/api'

interface AuditItem {
  logId: number
  module: string
  actionType: string
  entityType: string
  entityId?: string
  projectName?: string
  description: string
  oldValue?: string
  newValue?: string
  createdAt: string
}

interface AuditPage {
  items: AuditItem[]
  modules: string[]
  actionTypes: string[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const filters = ref({ search: '', module: '', actionType: '', startDate: '', endDate: '', page: 1, pageSize: 25 })
const data = ref<AuditPage>({ items: [], modules: [], actionTypes: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 1 })
const selected = ref<AuditItem | null>(null)
const loading = ref(false)
const error = ref('')

async function load(page = filters.value.page) {
  loading.value = true
  error.value = ''
  filters.value.page = page
  try {
    data.value = await api<AuditPage>(`/audit-logs${queryString(filters.value)}`)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入操作紀錄'
  } finally {
    loading.value = false
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium' }).format(new Date(value))
}

onMounted(() => load(1))
</script>

<template>
  <div class="page-stack">
    <div class="page-intro"><div><h2>操作紀錄</h2><p>追蹤主檔、專案、材料對應及估價設定的變更。</p></div><div class="api-badge">共 {{ data.totalCount }} 筆</div></div>
    <div v-if="error" class="notice error">{{ error }}</div>
    <section class="card">
      <form class="card-body toolbar" @submit.prevent="load(1)">
        <label class="field"><span>關鍵字</span><input v-model="filters.search" /></label>
        <label class="field"><span>模組</span><select v-model="filters.module"><option value="">全部</option><option v-for="item in data.modules" :key="item">{{ item }}</option></select></label>
        <label class="field"><span>動作</span><select v-model="filters.actionType"><option value="">全部</option><option v-for="item in data.actionTypes" :key="item">{{ item }}</option></select></label>
        <label class="field"><span>開始日期</span><input v-model="filters.startDate" type="date" /></label>
        <label class="field"><span>結束日期</span><input v-model="filters.endDate" type="date" /></label>
        <button class="button primary" :disabled="loading">查詢</button>
      </form>
      <div class="table-wrap"><table><thead><tr><th>時間</th><th>模組</th><th>動作</th><th>專案</th><th>說明</th><th></th></tr></thead><tbody>
        <tr v-for="item in data.items" :key="item.logId"><td>{{ formatDate(item.createdAt) }}</td><td>{{ item.module }}</td><td>{{ item.actionType }}</td><td>{{ item.projectName || '—' }}</td><td>{{ item.description }}</td><td><button class="button secondary small" @click="selected = item">差異</button></td></tr>
      </tbody></table><div v-if="!data.items.length && !loading" class="empty-state">目前沒有符合條件的操作紀錄</div></div>
      <div class="pagination"><span>第 {{ data.page }} / {{ data.totalPages }} 頁</span><div class="inline-actions"><button class="button secondary small" :disabled="data.page <= 1" @click="load(data.page - 1)">上一頁</button><button class="button secondary small" :disabled="data.page >= data.totalPages" @click="load(data.page + 1)">下一頁</button></div></div>
    </section>
    <section v-if="selected" class="card">
      <div class="card-header"><h3>變更內容 #{{ selected.logId }}</h3><button class="button secondary small" @click="selected = null">關閉</button></div>
      <div class="card-body two-column"><div><h4>變更前</h4><pre class="json-result">{{ selected.oldValue || '無資料' }}</pre></div><div><h4>變更後</h4><pre class="json-result">{{ selected.newValue || '無資料' }}</pre></div></div>
    </section>
  </div>
</template>
