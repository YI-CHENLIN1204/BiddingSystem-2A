<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api, apiForm, queryString } from '../services/api'
import type { ProjectForm } from '../types'

interface BidItem {
  finalBidItemId: number; category1?: string | null; category2?: string | null; category3?: string | null
  majorCategoryOverride?: string | null; effectiveMajorCategory?: string | null
  contributesStructuralPrice?: boolean; contributesQuantityHistory?: boolean; quantityHistoryCategory?: string | null
  itemName: string; spec?: string | null; unit?: string | null
  qty?: number | null; unitPrice?: number | null; materialId?: number | null
  materialName?: string | null; materialCategory?: string | null; mappingStatus: string
  isIgnored: boolean; invalidQuantity?: boolean; invalidUnitPrice?: boolean; unitMismatch?: boolean
}
interface MaterialOption { materialId: number; materialName: string; category?: string; unitName: string }
interface MappingSummary {
  totalCount: number; autoCount: number; manualCount: number; pendingCount: number
  invalidQuantityCount: number; invalidUnitPriceCount: number; unitMismatchCount: number; ignoredCount: number
}
interface ImportReport {
  projectId: number
  message?: string
  totalCount: number
  recognizedMajorCategoryCount: number
  unclassifiedMajorCategoryCount: number
  structuralItemCount: number
  structuralPricedItemCount: number
  structuralAmount: number
  historyInsertCount: number
  rebarHistoryInsertCount: number
  concreteHistoryInsertCount: number
}
interface PolicyResponse { knownMajorCategories: string[] }
interface FilterOptions { categories: string[]; units: string[] }
interface MasterOption { categoryId?: number; categoryName?: string; unitId?: number; unitName?: string; isActive: boolean }

const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({
  projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null, siteArea: null,
  floorCount: null, basementFloorCount: 0, projectMonth: '',
})
const file = ref<File | null>(null)
const fileInputKey = ref(0)
const uploading = ref(false)
const loadingMappings = ref(false)
const busyId = ref<number | null>(null)
const error = ref('')
const message = ref('')
const projectId = ref<number | null>(null)
const summary = ref<MappingSummary | null>(null)
const importReport = ref<ImportReport | null>(null)
const items = ref<BidItem[]>([])
const materialOptions = ref<MaterialOption[]>([])
const filters = ref<FilterOptions>({ categories: [], units: [] })
const masterOptions = ref<{ categories: MasterOption[]; units: MasterOption[] }>({ categories: [], units: [] })
const majorCategories = ref<string[]>([])
const status = ref('Pending')
const majorCategoryFilter = ref('All')
const issueCode = ref('All')
const category = ref('All')
const unit = ref('All')
const search = ref('')
const page = ref(1)
const pageSize = 50
const totalCount = ref(0)
const selectedCategory = ref<Record<number, string>>({})
const selectedMaterial = ref<Record<number, number | '__NEW__' | null>>({})
const editingItem = ref<BidItem | null>(null)
const editForm = ref({ unit: '', qty: null as number | null, unitPrice: null as number | null })
const creatingFor = ref<BidItem | null>(null)
const newMaterial = ref({ materialName: '', categoryId: null as number | null, unitId: null as number | null })
let searchTimer: number | undefined

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)))

function selectFile(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

function formatNumber(value?: number | null, digits = 2) {
  return value == null ? '無' : new Intl.NumberFormat('zh-TW', { maximumFractionDigits: digits }).format(value)
}

function materialsFor(item: BidItem) {
  const selected = selectedCategory.value[item.finalBidItemId]
  return materialOptions.value.filter(x => (x.category ?? '') === selected)
    .sort((a, b) => a.materialName.localeCompare(b.materialName, 'zh-Hant'))
}

function badge(item: BidItem) {
  if (item.isIgnored) return { label: '已忽略', css: 'bg-secondary' }
  if (item.mappingStatus === 'Auto') return { label: '自動對應', css: 'bg-success' }
  if (item.mappingStatus === 'Manual') return { label: '手動對應', css: 'bg-primary' }
  return { label: '未對應', css: 'bg-warning text-dark' }
}

async function upload() {
  if (!file.value) { error.value = '請選擇 Excel 檔案。'; return }
  uploading.value = true
  error.value = ''
  message.value = ''
  const form = new FormData()
  form.append('excelFile', file.value)
  Object.entries(project.value).forEach(([key, value]) => form.append(key, value == null ? '' : String(value)))
  try {
    const result = await apiForm<ImportReport>('/Estimation/upload-final-bid', form)
    projectId.value = Number(result.projectId)
    importReport.value = result
    message.value = result.message ?? '決標資料已成功回寫。'
    file.value = null
    fileInputKey.value++
    await loadMappingResources()
    await Promise.all([loadSummary(), loadItems(1)])
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '決標資料回寫失敗'
  } finally {
    uploading.value = false
  }
}

async function loadMappingResources() {
  if (!projectId.value) return
  const [materials, filterData, masters, policy] = await Promise.all([
    api<MaterialOption[]>('/Estimation/material-options'),
    api<FilterOptions>(`/Estimation/mapping-filter-options${queryString({ projectId: projectId.value })}`),
    api<{ categories: MasterOption[]; units: MasterOption[] }>('/standard-material/filter-options'),
    api<PolicyResponse>('/analysis-cases/policy'),
  ])
  materialOptions.value = materials
  filters.value = filterData
  masterOptions.value = masters
  majorCategories.value = policy.knownMajorCategories
}

async function loadSummary() {
  if (!projectId.value) return
  summary.value = await api<MappingSummary>(`/Estimation/mapping-summary${queryString({ projectId: projectId.value })}`)
}

async function loadItems(targetPage = page.value) {
  if (!projectId.value) return
  page.value = targetPage
  loadingMappings.value = true
  error.value = ''
  try {
    const result = await api<{ totalCount: number; items: BidItem[] }>(`/Estimation/mapping-items${queryString({
      projectId: projectId.value, status: status.value, issueCode: issueCode.value,
      majorCategory: majorCategoryFilter.value, category: category.value, unit: unit.value, search: search.value.trim(), page: page.value, pageSize,
    })}`)
    totalCount.value = result.totalCount
    items.value = result.items
    const categories: Record<number, string> = {}
    const materials: Record<number, number | '__NEW__' | null> = {}
    for (const item of result.items) {
      categories[item.finalBidItemId] = item.materialCategory ?? ''
      materials[item.finalBidItemId] = item.materialId ?? null
    }
    selectedCategory.value = categories
    selectedMaterial.value = materials
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '讀取材料對應資料失敗'
  } finally {
    loadingMappings.value = false
  }
}

async function refreshMappings(text?: string) {
  await Promise.all([loadSummary(), loadItems()])
  if (text) message.value = text
}

function changeCategory(item: BidItem) {
  if (!materialsFor(item).some(x => x.materialId === selectedMaterial.value[item.finalBidItemId])) {
    selectedMaterial.value[item.finalBidItemId] = null
  }
}

async function updateMajorCategory(item: BidItem, event: Event) {
  const value = (event.target as HTMLSelectElement).value
  busyId.value = item.finalBidItemId
  error.value = ''
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    form.append('majorCategory', value)
    await apiForm('/Estimation/update-major-category', form)
    await refreshMappings(value ? '工程大項已人工確認。' : '已恢復使用標單原始分類。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '工程大項更新失敗'
  } finally {
    busyId.value = null
  }
}

async function updateMapping(item: BidItem) {
  const materialId = selectedMaterial.value[item.finalBidItemId]
  if (materialId === '__NEW__') {
    openCreate(item)
    return
  }
  if (!materialId) { error.value = '請先選擇標準材料。'; return }
  busyId.value = item.finalBidItemId
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    form.append('materialId', String(materialId))
    await apiForm('/Estimation/update-material-mapping', form)
    await refreshMappings('材料對應已更新。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '材料對應失敗'
  } finally { busyId.value = null }
}

function openEdit(item: BidItem) {
  editingItem.value = item
  editForm.value = { unit: item.unit ?? '', qty: item.qty ?? null, unitPrice: item.unitPrice ?? null }
}

async function saveItem() {
  if (!editingItem.value) return
  busyId.value = editingItem.value.finalBidItemId
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(editingItem.value.finalBidItemId))
    form.append('itemName', editingItem.value.itemName)
    form.append('unit', editForm.value.unit)
    form.append('qty', editForm.value.qty == null ? '' : String(editForm.value.qty))
    form.append('unitPrice', editForm.value.unitPrice == null ? '' : String(editForm.value.unitPrice))
    await apiForm('/Estimation/update-bid-item-data', form)
    editingItem.value = null
    await refreshMappings('標單資料已修正。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '工項更新失敗'
  } finally { busyId.value = null }
}

async function ignore(item: BidItem) {
  if (!window.confirm(`確定要忽略「${item.itemName}」嗎？\n\n忽略後，此工項不會進入模型歷史資料。`)) return
  busyId.value = item.finalBidItemId
  try {
    const form = new FormData(); form.append('finalBidItemId', String(item.finalBidItemId))
    await apiForm('/Estimation/ignore-bid-item', form)
    await refreshMappings('工項已忽略。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '忽略工項失敗'
  } finally { busyId.value = null }
}

function openCreate(item: BidItem) {
  creatingFor.value = item
  const selected = masterOptions.value.categories.find(x => x.categoryName === selectedCategory.value[item.finalBidItemId])
  newMaterial.value = { materialName: '', categoryId: selected?.categoryId ?? null, unitId: null }
}

async function createMaterial() {
  if (!creatingFor.value || !newMaterial.value.categoryId || !newMaterial.value.unitId || !newMaterial.value.materialName.trim()) {
    error.value = '請完整填寫材料名稱、分類與單位。'; return
  }
  busyId.value = creatingFor.value.finalBidItemId
  try {
    const form = new FormData()
    form.append('materialName', newMaterial.value.materialName.trim())
    form.append('categoryId', String(newMaterial.value.categoryId))
    form.append('unitId', String(newMaterial.value.unitId))
    const result = await apiForm<{ materialId?: number }>('/standard-material/create', form)
    const target = creatingFor.value
    creatingFor.value = null
    await loadMappingResources()
    if (result.materialId) selectedMaterial.value[target.finalBidItemId] = result.materialId
    message.value = '標準材料已新增，請確認後執行人工對應。'
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '新增標準材料失敗'
  } finally { busyId.value = null }
}

function resetAndLoad() { page.value = 1; loadItems(1) }
watch(search, () => { window.clearTimeout(searchTimer); searchTimer = window.setTimeout(resetAndLoad, 300) })
watch([status, majorCategoryFilter, issueCode, category, unit], resetAndLoad)
</script>

<template>
  <div class="final-bid-page mt-4">
    <h2 class="page-title">決標單歷史回寫</h2>
    <div class="page-description">將已完成決標的標單匯入歷史資料庫，供後續工程用量估算與標單異常分析使用。</div>

    <div v-if="optionsError || error" class="alert alert-danger">{{ optionsError || error }}</div>
    <div v-if="message" class="alert alert-success alert-dismissible">{{ message }}<button type="button" class="btn-close" @click="message = ''"></button></div>

    <form class="admin-main-card" @submit.prevent="upload">
      <div class="form-section">
        <div class="form-section-header"><div class="form-section-title">1. 建案基本資料</div><div class="form-section-description">建立本次決標資料所屬的歷史建案條件。</div></div>
        <div class="form-section-body"><ProjectFields v-model="project" :options="options" include-name name-label="工程專案名稱" name-placeholder="例如：住宅新建工程" /></div>
      </div>
      <div class="form-section">
        <div class="form-section-header"><div class="form-section-title">2. 上傳最終決標標單</div><div class="form-section-description">支援 .xlsx、.xls、.xlsm、.xlsb、.csv 格式；解析完成後寫入歷史資料並進行材料對應。</div></div>
        <div class="form-section-body">
          <input :key="fileInputKey" id="finalBidFile" class="file-input-native" type="file" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" required @change="selectFile" />
          <label for="finalBidFile" class="file-picker"><span class="file-picker-button">選擇 Excel 檔案</span><span class="file-picker-name" :class="{ 'has-file': file }">{{ file?.name ?? '尚未選擇檔案' }}</span></label>
          <div class="upload-footer"><div class="upload-hint">請確認建案基本資料與標單檔案後再執行回寫。</div><button class="btn-wz-primary" type="submit" :disabled="uploading">{{ uploading ? '系統正在解析並同步，請稍候...' : '解析並回寫歷史資料' }}</button></div>
        </div>
      </div>
    </form>

    <div v-if="summary" class="mt-4">
      <div v-if="importReport" class="card mb-3 import-report">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-start gap-3 flex-wrap mb-3">
            <div>
              <h4 class="mb-1">2A 匯入資格檢查</h4>
              <div class="text-muted small">解析完成後立即檢查工程大項辨識、結構體價格與鋼筋／混凝土數量歷史。</div>
            </div>
            <RouterLink :to="`/projects/${importReport.projectId}`" class="btn btn-sm btn-outline-primary">查看建案完整摘要</RouterLink>
          </div>
          <div class="row g-2">
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">工程大項辨識</div><div class="stat-num">{{ importReport.recognizedMajorCategoryCount }}</div><small>/ {{ importReport.totalCount }} 筆</small></div></div>
            <div class="col-md-2"><div class="stat-box" :class="{ 'tone-warning': importReport.unclassifiedMajorCategoryCount > 0 }"><div class="text-muted fw-bold">未辨識大項</div><div class="stat-num">{{ importReport.unclassifiedMajorCategoryCount }}</div><small>可人工校正</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">結構體工項</div><div class="stat-num">{{ importReport.structuralItemCount }}</div><small>{{ importReport.structuralPricedItemCount }} 筆可計價</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">結構體總價</div><div class="stat-num report-amount">{{ formatNumber(importReport.structuralAmount) }}</div><small>價格歷史候選</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">鋼筋歷史</div><div class="stat-num">{{ importReport.rebarHistoryInsertCount }}</div><small>筆有效交易</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">混凝土歷史</div><div class="stat-num">{{ importReport.concreteHistoryInsertCount }}</div><small>筆有效交易</small></div></div>
          </div>
        </div>
      </div>

      <div class="alert alert-success shadow-sm result-summary">
        <h4 class="alert-heading fw-bold mb-2">建案材料對應摘要</h4>
        <div class="text-muted small mb-3">統計依目前資料庫狀態即時更新</div>
        <div class="row g-2 mb-3">
          <div v-for="entry in [
            { label: '總工項', value: summary.totalCount, tone: 'dark' },
            { label: '自動對應', value: summary.autoCount, tone: 'success' },
            { label: '手動對應', value: summary.manualCount, tone: 'primary' },
            { label: '待處理', value: summary.pendingCount, tone: 'warning' },
          ]" :key="entry.label" class="col-md-3"><div class="stat-box" :class="`tone-${entry.tone}`"><div class="fw-bold">{{ entry.label }}</div><div class="stat-num">{{ entry.value }}</div></div></div>
        </div>
        <div class="row g-2">
          <div v-for="entry in [
            { label: '數量無效', value: summary.invalidQuantityCount },
            { label: '單價無效', value: summary.invalidUnitPriceCount },
            { label: '單位不符', value: summary.unitMismatchCount },
            { label: '已忽略', value: summary.ignoredCount },
          ]" :key="entry.label" class="col-md-3"><div class="stat-box"><div class="text-muted fw-bold">{{ entry.label }}</div><div class="stat-num">{{ entry.value }}</div></div></div>
        </div>
      </div>

      <div class="mt-4">
        <div class="d-flex justify-content-between align-items-center mb-3"><div><h4 class="mb-1">標單審查與材料對應</h4><div class="text-muted small">先確認工程大項，再檢查鋼筋／混凝土材料與單位；原始分類永遠保留。</div></div><div class="d-flex align-items-center gap-3"><RouterLink v-if="projectId" :to="`/projects/${projectId}`" class="btn btn-sm btn-outline-primary">查看 2A 歷史基準摘要</RouterLink><div class="text-muted small">共 {{ totalCount }} 筆</div></div></div>
        <div class="card mb-3"><div class="card-body"><div class="mb-3"><input v-model="search" class="form-control" placeholder="搜尋工項名稱、規格、標準材料或建案名稱" /></div><div class="row g-2">
          <div class="col-md-3"><label class="form-label small fw-bold">對應狀態</label><select v-model="status" class="form-select"><option value="Pending">未對應</option><option value="Auto">自動對應</option><option value="Manual">手動對應</option><option value="Ignored">已忽略</option><option value="All">全部</option></select></div>
          <div class="col-md-3"><label class="form-label small fw-bold">待處理原因</label><select v-model="issueCode" class="form-select"><option value="All">全部</option><option value="INVALID_QUANTITY">數量無效</option><option value="INVALID_UNIT_PRICE">單價無效</option><option value="UNIT_MISMATCH">單位不符</option></select></div>
          <div class="col-md-3"><label class="form-label small fw-bold">材料分類</label><select v-model="category" class="form-select"><option value="All">全部</option><option v-for="entry in filters.categories" :key="entry" :value="entry">{{ entry }}</option></select></div>
          <div class="col-md-3"><label class="form-label small fw-bold">單位</label><select v-model="unit" class="form-select"><option value="All">全部</option><option v-for="entry in filters.units" :key="entry" :value="entry">{{ entry }}</option></select></div>
        </div></div></div>

        <div v-if="loadingMappings" class="card p-4 text-muted">載入材料對應資料中...</div>
        <div v-else-if="!items.length" class="card p-4 text-muted">沒有符合條件的工項</div>
        <article v-for="item in items" v-else :key="item.finalBidItemId" class="card mb-3 mapping-card"><div class="card-body">
          <div class="d-flex justify-content-between align-items-start gap-3 mb-2"><h5 class="mb-1">{{ item.itemName }}</h5><div class="d-flex gap-1 flex-wrap justify-content-end"><span v-if="item.effectiveMajorCategory" class="badge bg-info text-dark">{{ item.effectiveMajorCategory }}</span><span v-if="item.contributesStructuralPrice" class="badge bg-success">結構體價格基準</span><span v-if="item.contributesQuantityHistory" class="badge bg-success">{{ item.quantityHistoryCategory }}數量基準</span><span v-if="!item.contributesStructuralPrice && !item.contributesQuantityHistory" class="badge bg-light text-dark border">不納入目前 2A 基準</span><span v-if="item.majorCategoryOverride" class="badge bg-primary">人工大項</span><span class="badge" :class="badge(item).css">{{ badge(item).label }}</span><span v-if="item.invalidQuantity" class="badge bg-danger">數量無效</span><span v-if="item.invalidUnitPrice" class="badge bg-danger">單價無效</span><span v-if="item.unitMismatch" class="badge bg-danger">單位不符</span></div></div>
          <div class="text-muted small mb-2">單位：{{ item.unit || '無' }} ｜ 數量：{{ formatNumber(item.qty, 4) }} ｜ 單價：{{ formatNumber(item.unitPrice) }}</div>
          <div class="text-muted small mb-3">原始分類：{{ [item.category1, item.category2, item.category3].filter(Boolean).join(' → ') || '未辨識' }}</div>
          <div v-if="!item.isIgnored" class="row g-2 mb-3"><div class="col-md-6"><label class="form-label small fw-bold">工程大項</label><select class="form-select" :value="item.majorCategoryOverride ?? ''" :disabled="busyId === item.finalBidItemId" @change="updateMajorCategory(item, $event)"><option value="">使用原始分類（{{ item.effectiveMajorCategory || '未辨識' }}）</option><option v-for="entry in majorCategories" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-md-6 d-flex align-items-end"><div class="small text-muted">人工指定只影響 2A 判定，原始 Category1 / 2 / 3 不會被覆蓋。</div></div></div>
          <div class="d-flex justify-content-between align-items-center gap-2 mb-2 flex-wrap"><div class="small"><strong>目前標準材料：</strong><span v-if="item.materialName">{{ item.materialName }}</span><span v-else class="text-warning">尚未對應</span></div><div class="d-flex gap-2"><button class="btn btn-sm btn-outline-secondary" type="button" @click="openEdit(item)">修正標單資料</button><button v-if="!item.isIgnored" class="btn btn-sm btn-outline-danger" type="button" @click="ignore(item)">忽略</button></div></div>
          <div v-if="!item.isIgnored" class="row g-2"><div class="col-md-4"><label class="form-label small fw-bold">材料分類</label><select v-model="selectedCategory[item.finalBidItemId]" class="form-select" @change="changeCategory(item)"><option value="">請選擇材料分類</option><option v-for="entry in filters.categories" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-md-5"><label class="form-label small fw-bold">標準材料</label><select v-model="selectedMaterial[item.finalBidItemId]" class="form-select" :disabled="!selectedCategory[item.finalBidItemId]"><option :value="null">請選擇標準材料</option><option v-for="material in materialsFor(item)" :key="material.materialId" :value="material.materialId">{{ material.materialName }} ({{ material.unitName }})</option><option value="__NEW__">＋ 新增標準材料</option></select></div><div class="col-md-3"><label class="form-label small fw-bold">&nbsp;</label><button class="btn btn-outline-primary w-100" type="button" :disabled="busyId === item.finalBidItemId" @click="updateMapping(item)">{{ selectedMaterial[item.finalBidItemId] === '__NEW__' ? '新增標準材料' : item.mappingStatus === 'Pending' ? '人工對應' : '修改對應' }}</button></div></div>
        </div></article>
        <div class="d-flex justify-content-between align-items-center mt-3"><button class="btn btn-outline-secondary btn-sm" :disabled="page <= 1" @click="loadItems(page - 1)">上一頁</button><div class="text-muted small">第 {{ page }} / {{ totalPages }} 頁</div><button class="btn btn-outline-secondary btn-sm" :disabled="page >= totalPages" @click="loadItems(page + 1)">下一頁</button></div>
      </div>
    </div>

    <div v-if="editingItem" class="modal-backdrop-custom" @mousedown.self="editingItem = null"><div class="modal-dialog"><form class="modal-content" @submit.prevent="saveItem"><div class="modal-header"><h5 class="modal-title">修正標單資料</h5><button type="button" class="btn-close" @click="editingItem = null"></button></div><div class="modal-body"><div class="mb-3"><label class="form-label fw-bold">工項</label><div class="form-control-plaintext">{{ editingItem.itemName }}</div></div><div class="mb-3"><label class="form-label fw-bold">數量</label><input v-model.number="editForm.qty" type="number" step="any" class="form-control" /></div><div class="mb-3"><label class="form-label fw-bold">單價</label><input v-model.number="editForm.unitPrice" type="number" step="any" class="form-control" /></div><div class="mb-3"><label class="form-label fw-bold">單位</label><select v-model="editForm.unit" class="form-select"><option value="">請選擇單位</option><option v-for="entry in filters.units" :key="entry" :value="entry">{{ entry }}</option></select></div></div><div class="modal-footer"><button type="button" class="btn btn-outline-secondary" @click="editingItem = null">取消</button><button type="submit" class="btn btn-primary">儲存修改</button></div></form></div></div>

    <div v-if="creatingFor" class="modal-backdrop-custom" @mousedown.self="creatingFor = null"><div class="modal-dialog"><form class="modal-content" @submit.prevent="createMaterial"><div class="modal-header"><h5 class="modal-title">新增標準材料</h5><button type="button" class="btn-close" @click="creatingFor = null"></button></div><div class="modal-body"><div class="mb-3"><label class="form-label fw-bold">材料名稱</label><input v-model="newMaterial.materialName" class="form-control" required /></div><div class="mb-3"><label class="form-label fw-bold">材料分類</label><select v-model="newMaterial.categoryId" class="form-select" required><option :value="null">請選擇材料分類</option><option v-for="entry in masterOptions.categories.filter(x => x.isActive)" :key="entry.categoryId" :value="entry.categoryId">{{ entry.categoryName }}</option></select></div><div class="mb-3"><label class="form-label fw-bold">標準單位</label><select v-model="newMaterial.unitId" class="form-select" required><option :value="null">請選擇標準單位</option><option v-for="entry in masterOptions.units.filter(x => x.isActive)" :key="entry.unitId" :value="entry.unitId">{{ entry.unitName }}</option></select></div></div><div class="modal-footer"><button type="button" class="btn btn-outline-secondary" @click="creatingFor = null">取消</button><button type="submit" class="btn btn-primary">新增</button></div></form></div></div>
  </div>
</template>

<style scoped>
.final-bid-page { max-width: 1280px; margin: 0 auto; padding-bottom: 40px; }
.page-title { font-size: 1.55rem; font-weight: 800; margin-bottom: 4px; }
.page-description { color: #6c757d; font-size: 14px; margin-bottom: 18px; }
.admin-main-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; box-shadow: 0 2px 8px rgba(20,29,35,.06); overflow: hidden; }
.form-section + .form-section { border-top: 1px solid #d8dbe0; }
.form-section-header { padding: 12px 16px; background: #f8f9fa; border-bottom: 1px solid #e3e5e8; }
.form-section-title { color: #321fdb; font-size: 14px; font-weight: 800; }
.form-section-description { margin-top: 2px; color: #6c757d; font-size: 11px; }
.form-section-body { padding: 16px; }
.final-bid-page :deep(.form-grid) { grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.final-bid-page :deep(.form-grid .span-2:first-child) { grid-column: 1 / -1; }
.final-bid-page :deep(.field) { min-width: 0; gap: 0; }
.final-bid-page :deep(.field > span) { display: block; margin-bottom: 6px; color: #3c4b64; font-size: 12px; font-weight: 800; }
.final-bid-page :deep(.field input), .final-bid-page :deep(.field select) { width: 100%; height: 38px; min-height: 38px; padding: 6px 12px; color: #141d23; background-color: #fff; border: 1px solid #d8dbe0; border-radius: 6px; font-size: 14px; line-height: 1.5; transition: border-color .15s ease, box-shadow .15s ease; }
.final-bid-page :deep(.field input::placeholder) { color: #9aa5b1; }
.final-bid-page :deep(.field input:focus), .final-bid-page :deep(.field select:focus) { border-color: #321fdb; box-shadow: 0 0 0 .15rem rgba(50,31,219,.12); outline: 0; }
.file-input-native { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; border: 0; }
.file-picker { width: 100%; min-height: 42px; border-radius: 6px; border: 1px solid #d8dbe0; background: #fff; display: flex; align-items: center; overflow: hidden; cursor: pointer; }
.file-picker-button { align-self: stretch; padding: 0 18px; background: #ebe8ff; color: #321fdb; border-right: 1px solid #d8dbe0; display: inline-flex; align-items: center; font-weight: 800; font-size: 13px; }
.file-picker-name { color: #6c757d; padding: 0 14px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 13px; }
.file-picker-name.has-file { color: #141d23; font-weight: 600; }
.upload-footer { display: flex; justify-content: space-between; align-items: center; gap: 16px; margin-top: 14px; }
.upload-hint { color: #6c757d; font-size: 12px; }
.btn-wz-primary { background: #321fdb; color: #fff; border-radius: 6px; padding: 9px 20px; font-weight: 800; border: 0; }
.import-report { border-left: 5px solid #198754; }.report-amount{font-size:1.2rem!important;line-height:2.2rem}.result-summary { background: #fff; border-left: 5px solid #321fdb; }
.result-summary .alert-heading { color: #321fdb; }
.stat-box { height: 100%; background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 6px; padding: 14px; text-align: center; }
.stat-num { font-size: 2rem; font-weight: 900; }
.tone-success { color: #198754; border-color: #198754; background: #f1faf5; }
.tone-primary { color: #321fdb; border-color: #321fdb; background: #f3f1ff; }
.tone-warning { color: #b58100; border-color: #ffc107; background: #fff9e6; }
.modal-backdrop-custom { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; background: rgba(0,0,0,.5); }
.modal-backdrop-custom .modal-dialog { width: min(500px, 100%); margin: auto; }
@media (max-width: 1000px) { .final-bid-page :deep(.form-grid) { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 650px) { .final-bid-page :deep(.form-grid) { grid-template-columns: 1fr; } .final-bid-page :deep(.form-grid .span-2:first-child) { grid-column: auto; } .file-picker, .upload-footer { flex-direction: column; align-items: stretch; } .file-picker-button { min-height: 42px; border-right: 0; border-bottom: 1px solid #d8dbe0; justify-content: center; } .file-picker-name { padding: 12px 16px; } }
</style>
