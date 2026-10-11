<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, queryString } from '../services/api'

interface AuditItem {
  logId: number; module: string; actionType: string; entityType: string; entityId?: string
  projectId?: number; projectName?: string; description: string; oldValue?: string; newValue?: string; createdAt: string
}
interface AuditPage { items: AuditItem[]; modules: string[]; actionTypes: string[]; page: number; pageSize: number; totalCount: number; totalPages: number }

const actionLabels: Record<string, string> = {
  Test: '測試', UpdateMapping: '更新材料對應', UpdateBidItem: '修改決標工項', IgnoreBidItem: '忽略決標工項', UnignoreBidItem: '取消忽略決標工項',
  CreateMaterial: '新增標準材料', UpdateMaterial: '修改標準材料', DisableMaterial: '停用標準材料', EnableMaterial: '啟用標準材料',
  CreateCategory: '新增材料分類', UpdateCategory: '修改材料分類', DisableCategory: '停用材料分類', EnableCategory: '啟用材料分類',
  CreateUnitCategory: '新增單位類別', CreateUnit: '新增單位', UpdateUnit: '修改單位', DisableUnit: '停用單位', EnableUnit: '啟用單位',
  UpdateEstimationSettings: '修改估算參數', EnableCoreMaterial: '設為核心材料', DisableCoreMaterial: '取消核心材料',
  UpdateProject: '修改建案', DeleteProject: '刪除建案', ImportFinalBid: '決標標單回寫', AnalyzeBidRange: '執行異常偵測',
}
const entityLabels: Record<string, string> = {
  System: '系統', FinalBidItem: '決標工項', MaterialMaster: '標準材料', MaterialCategory: '材料分類', UnitMaster: '標準單位',
  UnitCategory: '單位類別', EstimationSetting: '估算參數', QuantityRuleBase: '核心材料設定', ProjectMaster: '建案', BidRangeAnomalyAnalysis: '異常偵測分析',
}
const fieldLabels: Record<string, string> = {
  materialId: '材料編號', materialName: '材料名稱', mappingStatus: '對應狀態', itemName: '工項名稱', qty: '數量', unitPrice: '單價', unit: '標單單位',
  dataIssueCode: '資料問題代碼', dataIssueMessage: '資料問題說明', historyAction: '歷史交易處理', categoryId: '分類編號', categoryName: '分類名稱',
  unitId: '單位編號', unitName: '單位名稱', unitCategoryId: '單位類別編號', allowConversion: '允許換算', toBaseFactor: '換算倍率', isBaseUnit: '是否基準單位',
  isActive: '啟用狀態', isIgnored: '忽略狀態', historyDeleted: '歷史交易已刪除', projectId: '建案編號', projectName: '建案名稱', projectType: '建案類型',
  structureType: '結構類型', city: '縣市', totalFloorArea: '總樓地板面積', floorCount: '地上樓層', basementFloorCount: '地下樓層',
  sourceFileName: '來源檔案', fileName: '檔案名稱', totalCount: '工項總數', autoMappedCount: '自動對應數', unmappedCount: '未對應數', historyInsertCount: '新增歷史交易數',
  topK: 'Top K', minimumSampleCount: '最低樣本數', lowerQuantile: '下界分位數', medianQuantile: '中位分位數', upperQuantile: '上界分位數',
  similarityExponent: '相似度指數', timeHalfLifeYears: '時間半衰期（年）', minimumSimilarityScore: '最低相似度', structureWeight: '結構權重', areaWeight: '面積權重',
  floorWeight: '樓層權重', yearWeight: '年份權重', cityWeight: '縣市權重', isCoreEstimation: '核心材料',
}

const filters = ref({ search: '', module: '', actionType: '', startDate: '', endDate: '', page: 1, pageSize: 25 })
const data = ref<AuditPage>({ items: [], modules: [], actionTypes: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 1 })
const selected = ref<AuditItem | null>(null)
const loading = ref(false)
const error = ref('')
const visiblePages = computed(() => {
  const start = Math.max(1, data.value.page - 2)
  const end = Math.min(data.value.totalPages, data.value.page + 2)
  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
})

function actionLabel(value: string) { return actionLabels[value] ?? value }
function entityLabel(value: string) { return entityLabels[value] ?? value }
function fieldLabel(value: string) { return fieldLabels[value] ?? value.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/_/g, ' ') }
function formatDate(value: string) { return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(new Date(value)) }

function displayValue(value: unknown): string {
  if (value == null || value === '') return '—'
  if (typeof value === 'boolean') return value ? '是' : '否'
  if (Array.isArray(value)) return value.length ? value.map(displayValue).join('；') : '—'
  if (typeof value === 'object') return Object.entries(value as Record<string, unknown>).map(([key, entry]) => `${fieldLabel(key)}：${displayValue(entry)}`).join('、')
  return String(value)
}

function parseChange(raw?: string): { label: string; value: string }[] {
  if (!raw?.trim()) return []
  try {
    const parsed: unknown = JSON.parse(raw)
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return Object.entries(parsed as Record<string, unknown>).map(([key, value]) => ({ label: fieldLabel(key), value: displayValue(value) }))
    }
    return [{ label: '內容', value: displayValue(parsed) }]
  } catch { return [{ label: '內容', value: raw }] }
}

async function load(page = filters.value.page) {
  loading.value = true; error.value = ''; filters.value.page = page
  try { data.value = await api<AuditPage>(`/audit-logs${queryString(filters.value)}`) }
  catch (caught) { error.value = caught instanceof Error ? caught.message : '無法載入操作紀錄' }
  finally { loading.value = false }
}

function reset() {
  filters.value = { search: '', module: '', actionType: '', startDate: '', endDate: '', page: 1, pageSize: filters.value.pageSize }
  load(1)
}

onMounted(() => load(1))
</script>

<template>
  <div class="audit-page">
    <div class="audit-title-row"><div><h1 class="audit-title">操作紀錄</h1><div class="audit-subtitle">查詢系統內各項資料異動與分析操作紀錄</div></div><div class="audit-count">共 {{ data.totalCount }} 筆</div></div>
    <div v-if="error" class="alert alert-danger">{{ error }}</div>
    <form class="audit-filter-card" @submit.prevent="load(1)"><div class="audit-filter-grid"><label><span>關鍵字</span><input v-model="filters.search" class="form-control" placeholder="說明、模組、操作、Entity ID..." /></label><label><span>模組</span><select v-model="filters.module" class="form-select"><option value="">全部模組</option><option v-for="item in data.modules" :key="item" :value="item">{{ item }}</option></select></label><label><span>操作類型</span><select v-model="filters.actionType" class="form-select"><option value="">全部操作</option><option v-for="item in data.actionTypes" :key="item" :value="item">{{ actionLabel(item) }}</option></select></label><label><span>開始日期</span><input v-model="filters.startDate" type="date" class="form-control" /></label><label><span>結束日期</span><input v-model="filters.endDate" type="date" class="form-control" /></label><div class="d-flex gap-2"><button class="btn btn-primary" :disabled="loading">查詢</button><button class="btn btn-outline-secondary" type="button" @click="reset">清除</button></div></div></form>
    <div class="audit-table-card"><div class="audit-table-wrap"><table class="table audit-table"><thead><tr><th>時間</th><th>模組</th><th>操作</th><th>說明</th><th>建案</th><th>詳細</th></tr></thead><tbody><tr v-for="item in data.items" :key="item.logId"><td>{{ formatDate(item.createdAt) }}</td><td><span class="audit-module">{{ item.module }}</span></td><td><span class="audit-action" :title="item.actionType">{{ actionLabel(item.actionType) }}</span></td><td class="audit-description">{{ item.description }}</td><td class="audit-project"><div>{{ item.projectName || '—' }}</div><div v-if="item.projectId" class="audit-project-id">Project ID：{{ item.projectId }}</div></td><td><button class="btn btn-sm btn-outline-primary" @click="selected = item">查看</button></td></tr><tr v-if="!data.items.length"><td colspan="6" class="audit-empty">{{ loading ? '載入中...' : '沒有符合條件的操作紀錄' }}</td></tr></tbody></table></div>
      <div class="audit-footer"><div class="audit-page-info">第 {{ data.page }} / {{ data.totalPages }} 頁</div><div class="d-flex align-items-center gap-3"><select v-model.number="filters.pageSize" class="form-select form-select-sm page-size" @change="load(1)"><option :value="25">25 筆</option><option :value="50">50 筆</option><option :value="100">100 筆</option></select><nav><ul class="pagination pagination-sm mb-0"><li class="page-item" :class="{ disabled: data.page <= 1 }"><button class="page-link" @click="load(data.page - 1)">上一頁</button></li><li v-for="pageNumber in visiblePages" :key="pageNumber" class="page-item" :class="{ active: pageNumber === data.page }"><button class="page-link" @click="load(pageNumber)">{{ pageNumber }}</button></li><li class="page-item" :class="{ disabled: data.page >= data.totalPages }"><button class="page-link" @click="load(data.page + 1)">下一頁</button></li></ul></nav></div></div>
    </div>

    <div v-if="selected" class="modal-layer" @mousedown.self="selected = null"><div class="modal-dialog modal-lg" role="dialog" aria-modal="true" aria-labelledby="audit-detail-title"><div class="modal-content"><div class="modal-header"><div><h5 id="audit-detail-title" class="modal-title">操作紀錄詳細內容</h5><div class="text-muted small">Log ID：{{ selected.logId }}</div></div><button type="button" class="btn-close" aria-label="關閉" @click="selected = null"></button></div><div class="modal-body"><div class="audit-detail-meta"><div class="audit-meta-item"><span>模組</span><strong>{{ selected.module }}</strong></div><div class="audit-meta-item"><span>操作</span><strong>{{ actionLabel(selected.actionType) }}</strong></div><div class="audit-meta-item"><span>資料類型</span><strong>{{ entityLabel(selected.entityType) }}</strong></div><div class="audit-meta-item"><span>資料編號</span><strong>{{ selected.entityId || '—' }}</strong></div></div><div class="mb-4"><div class="fw-bold mb-2">操作說明</div><div>{{ selected.description || '—' }}</div></div><div class="mb-4"><div class="change-title">修改前</div><div class="change-box"><div v-if="!parseChange(selected.oldValue).length" class="change-empty">無資料</div><table v-else><tbody><tr v-for="entry in parseChange(selected.oldValue)" :key="entry.label"><th>{{ entry.label }}</th><td>{{ entry.value }}</td></tr></tbody></table></div></div><div><div class="change-title">修改後</div><div class="change-box"><div v-if="!parseChange(selected.newValue).length" class="change-empty">無資料</div><table v-else><tbody><tr v-for="entry in parseChange(selected.newValue)" :key="entry.label"><th>{{ entry.label }}</th><td>{{ entry.value }}</td></tr></tbody></table></div></div></div></div></div></div>
  </div>
</template>

<style scoped>
.audit-page { display: flex; flex-direction: column; gap: 16px; }.audit-title-row { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }.audit-title { margin: 0; font-size: 26px; font-weight: 800; }.audit-subtitle,.audit-count { color: #6c757d; font-size: 14px; }.audit-filter-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 6px; padding: 18px; }.audit-filter-grid { display: grid; grid-template-columns: minmax(240px,2fr) repeat(2,minmax(170px,1fr)) repeat(2,minmax(150px,.9fr)) auto; gap: 12px; align-items: end; }.audit-filter-grid label > span { display: block; margin-bottom: 6px; color: #5b6573; font-size: 13px; font-weight: 700; }
.audit-table-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 6px; overflow: hidden; }.audit-table-wrap { overflow-x: auto; }.audit-table { min-width: 980px; margin: 0; }.audit-table thead th { padding: 12px 14px; background: #f7f8fa; color: #4f5965; font-size: 13px; font-weight: 800; }.audit-table tbody td { padding: 12px 14px; vertical-align: middle; border-color: #eef0f2; font-size: 14px; }.audit-module { font-weight: 700; }.audit-action { display: inline-block; padding: 4px 8px; border: 1px solid #d9dce1; border-radius: 999px; background: #f8f9fa; color: #45505d; font-size: 12px; font-weight: 700; white-space: nowrap; }.audit-description { min-width: 280px; max-width: 460px; }.audit-project { min-width: 150px; }.audit-project-id { color: #8a949e; font-size: 12px; }.audit-empty { padding: 48px 20px !important; text-align: center; color: #7b8490; }.audit-footer { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 14px 16px; border-top: 1px solid #d8dbe0; }.page-size { width: 84px; }
.modal-layer { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; background: rgba(0,0,0,.5); }.modal-layer .modal-dialog { width: min(800px,100%); max-height: calc(100vh - 3rem); margin: auto; }.modal-content { max-height: calc(100vh - 3rem); overflow-y: auto; }.audit-detail-meta { display: grid; grid-template-columns: repeat(2,1fr); gap: 12px; margin-bottom: 16px; }.audit-meta-item { padding: 10px 12px; border: 1px solid #e8eaed; border-radius: 6px; background: #fbfbfc; }.audit-meta-item span { display: block; color: #7b8490; font-size: 12px; }.audit-meta-item strong { display: block; color: #27313c; font-size: 14px; }.change-title { margin-bottom: 8px; font-weight: 800; }.change-box { border: 1px solid #e2e5e9; border-radius: 6px; overflow: hidden; }.change-box table { width: 100%; }.change-box th,.change-box td { padding: 10px 12px; border-bottom: 1px solid #edf0f2; text-align: left; font-size: 13px; white-space: normal; }.change-box th { width: 38%; background: #f8f9fa; color: #56606c; }.change-empty { padding: 18px; color: #7b8490; text-align: center; }
@media (max-width: 1200px) { .audit-filter-grid { grid-template-columns: repeat(2,minmax(180px,1fr)); } } @media (max-width: 650px) { .audit-filter-grid,.audit-detail-meta { grid-template-columns: 1fr; }.audit-footer { align-items: stretch; flex-direction: column; } }
</style>
