<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectFields from '../components/ProjectFields.vue'
import WorkbookSheetPicker from '../components/WorkbookSheetPicker.vue'
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
interface ProjectTwoASummary {
  projectId: number
  historyStatus: string
  structuralItemCount: number
  structuralPricedItemCount: number
  structuralUnpricedItemCount: number
  unclassifiedItemCount: number
  manualMajorCategoryCount: number
  structuralAmount: number
  rebarSourceItemCount: number
  rebarCandidateItemCount: number
  rebarExcludedItemCount: number
  concreteSourceItemCount: number
  concreteCandidateItemCount: number
  concreteExcludedItemCount: number
  publishReady: boolean
  publishBlockingIssues: string[]
}
interface PolicyResponse { knownMajorCategories: string[] }
interface FilterOptions { categories: string[]; units: string[] }
interface MasterOption { categoryId?: number; categoryName?: string; unitId?: number; unitName?: string; isActive: boolean }

const route = useRoute()
const router = useRouter()
const { options, error: optionsError } = useSystemOptions()
const project = ref<ProjectForm>({
  projectName: '', projectType: '', structureType: '', city: '', totalFloorArea: null, siteArea: null,
  floorCount: null, basementFloorCount: 0, projectMonth: '',
})
const file = ref<File | null>(null)
const includedSheets = ref<string[]>([])
const workbookReady = ref(false)
const fileInputKey = ref(0)
const uploading = ref(false)
const loadingMappings = ref(false)
const busyId = ref<number | null>(null)
const error = ref('')
const message = ref('')
const projectId = ref<number | null>(null)
const summary = ref<MappingSummary | null>(null)
const twoASummary = ref<ProjectTwoASummary | null>(null)
const publishingHistory = ref(false)
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
  includedSheets.value = []
  workbookReady.value = false
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
  if (!workbookReady.value || !includedSheets.value.length) {
    error.value = '請先完成工作表預覽，並勾選要匯入的工程明細。'; return
  }
  uploading.value = true
  error.value = ''
  message.value = ''
  const form = new FormData()
  form.append('excelFile', file.value)
  form.append('includedSheetNames', JSON.stringify(includedSheets.value))
  Object.entries(project.value).forEach(([key, value]) => form.append(key, value == null ? '' : String(value)))

  try {
    const result = await apiForm<{ projectId: number; message?: string }>('/Estimation/upload-final-bid', form)
    const createdId = Number(result.projectId)
    if (!Number.isSafeInteger(createdId) || createdId <= 0) {
      error.value = '後端已回應匯入，但未提供有效建案編號。請先到建案管理查詢，勿直接重傳。'
      return
    }
    projectId.value = createdId
    message.value = '決標資料已匯入審核草稿；尚未正式發布歷史基準。'
    file.value = null
    includedSheets.value = []
    workbookReady.value = false
    fileInputKey.value++

    // 匯入成功之後的導航或重新讀取失敗，不應誤報成上傳失敗。
    try {
      await router.replace({ path: '/final-bid', query: { projectId: String(createdId) } })
    } catch {
      error.value = '草稿已建立，但網址未能更新。可從建案管理重新進入審核。'
    }
    try {
      await loadMappingResources()
      await Promise.all([loadSummary(), loadTwoASummary(), loadItems(1)])
    } catch (caught) {
      error.value = '草稿已建立，但讀取審核明細失敗：' +
        (caught instanceof Error ? caught.message : '未知錯誤')
    }
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '匯入決標草稿失敗'
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

async function loadTwoASummary() {
  if (!projectId.value) return
  twoASummary.value = await api<ProjectTwoASummary>(`/projects/${projectId.value}/2a-summary`)
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
  await Promise.all([loadSummary(), loadTwoASummary(), loadItems()])
  if (text) message.value = text
}

async function publishHistory() {
  if (publishingHistory.value || !projectId.value || !twoASummary.value?.publishReady) return
  if (!window.confirm(
    '確認將此建案納入 2A 歷史基準嗎？\n\n發布後，本案會開始參與相似建案、結構體價格與鋼筋／混凝土數量模型。'
  )) return

  const id = projectId.value
  const receiptKey = 'history-operation:' + id + ':publish-history'
  let operationId = sessionStorage.getItem(receiptKey)
  if (!operationId) {
    operationId = crypto.randomUUID()
    sessionStorage.setItem(receiptKey, operationId)
  }

  publishingHistory.value = true
  error.value = ''
  message.value = ''
  let published = false
  try {
    await api('/projects/' + id + '/publish-history', {
      method: 'POST',
      headers: { 'Idempotency-Key': operationId },
    })
    published = true
  } catch (caught) {
    // HTTP timeout does not imply transaction failure. Verify the receipt
    // committed in the same DB transaction before reporting the outcome.
    try {
      const receipt = await api<{ actionType: string }>(
        '/projects/' + id + '/history-operation/' + operationId,
      )
      published = receipt.actionType === 'PublishHistory'
      if (!published) throw new Error('歷史操作收據與發布動作不符')
    } catch {
      const reason = caught instanceof Error ? caught.message : '網路連線失敗'
      error.value = reason + '；發布結果尚未確認，已保留同一操作識別碼。請在建案詳情核對狀態，勿建立新的發布操作。'
    }
  } finally {
    if (published) {
      sessionStorage.removeItem(receiptKey)
      try {
        await Promise.all([loadSummary(), loadTwoASummary(), loadItems()])
        message.value = '此建案已確認並納入 2A 歷史基準。'
      } catch (caught) {
        error.value = '歷史發布已確認成功，但重新載入審核資料失敗：' +
          (caught instanceof Error ? caught.message : '未知錯誤')
      }
    }
    publishingHistory.value = false
  }
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

async function excludeQuantityHistory(item: BidItem) {
  if (!window.confirm(`確定「${item.itemName}」只排除鋼筋／混凝土數量歷史？\n工程大項價格與原始工項仍保留。`)) return
  busyId.value = item.finalBidItemId
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    await apiForm('/Estimation/exclude-quantity-history', form)
    await refreshMappings('已保留工程價格，僅排除材料數量歷史。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '數量歷史排除失敗'
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

async function openExistingReview() {
  const raw = route.query.projectId
  if (raw == null) {
    projectId.value = null
    summary.value = null
    twoASummary.value = null
    items.value = []
    return
  }
  if (typeof raw !== 'string' || !/^[1-9][0-9]*$/.test(raw) || !Number.isSafeInteger(Number(raw))) {
    projectId.value = null
    summary.value = null
    twoASummary.value = null
    items.value = []
    error.value = '決標審核的建案編號無效'
    return
  }
  projectId.value = Number(raw)
  error.value = ''
  try {
    await loadMappingResources()
    await Promise.all([loadSummary(), loadTwoASummary(), loadItems(1)])
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法重新載入此建案決標審核'
  }
}
// 同一個頁面可切換不同建案 query，避免顯示前一案的審核資料。
watch(() => route.query.projectId, () => {
  if (!uploading.value) void openExistingReview()
}, { immediate: true })

function resetAndLoad() { page.value = 1; loadItems(1) }
watch(search, () => { window.clearTimeout(searchTimer); searchTimer = window.setTimeout(resetAndLoad, 300) })
watch([status, majorCategoryFilter, issueCode, category, unit], resetAndLoad)
</script>

<template>
  <div class="final-bid-page mt-4">
    <h2 class="page-title">決標單歷史回寫</h2>
    <div class="page-description">將已完成決標的最終標單匯入審核區；完成工程大項、材料與單位檢查並人工發布後，才會進入 2A 歷史模型。</div>

    <div v-if="optionsError || error" class="alert alert-danger">{{ optionsError || error }}</div>
    <div v-if="message" class="alert alert-success alert-dismissible">{{ message }}<button type="button" class="btn-close" @click="message = ''"></button></div>

    <form class="admin-main-card" @submit.prevent="upload">
      <div class="form-section">
        <div class="form-section-header"><div class="form-section-title">1. 建案基本資料</div><div class="form-section-description">建立本次決標資料所屬的歷史建案條件。</div></div>
        <div class="form-section-body"><ProjectFields v-model="project" :options="options" include-name name-label="工程專案名稱" name-placeholder="例如：住宅新建工程" /></div>
      </div>
      <div class="form-section">
        <div class="form-section-header"><div class="form-section-title">2. 上傳最終決標標單</div><div class="form-section-description">支援 .xlsx、.xls、.xlsm、.xlsb、.csv 格式；解析後先建立審核草稿與材料對應，不會立即影響模型。</div></div>
        <div class="form-section-body">
          <input :key="fileInputKey" id="finalBidFile" class="file-input-native" type="file" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" required @change="selectFile" />
          <label for="finalBidFile" class="file-picker"><span class="file-picker-button">選擇 Excel 檔案</span><span class="file-picker-name" :class="{ 'has-file': file }">{{ file?.name ?? '尚未選擇檔案' }}</span></label>
          <WorkbookSheetPicker :file="file" v-model="includedSheets" @update:ready="workbookReady = $event" />
          <div class="upload-footer"><div class="upload-hint">請確認建案基本資料與標單檔案後再執行回寫。</div><button class="btn-wz-primary" type="submit" :disabled="uploading || !workbookReady || !includedSheets.length">{{ uploading ? '系統正在解析並建立草稿，請稍候...' : '匯入並建立審核草稿' }}</button></div>
        </div>
      </div>
    </form>

    <div v-if="summary" class="mt-4">
      <div v-if="twoASummary && summary" class="card mb-3 import-report">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-start gap-3 flex-wrap mb-3">
            <div>
              <h4 class="mb-1">2A 歷史基準審核</h4>
              <div class="text-muted small">此區會隨工程大項、材料與標單資料修正即時更新；發布前不會影響模型。</div>
            </div>
            <div class="d-flex gap-2">
              <RouterLink v-if="projectId" :to="`/projects/${projectId}`" class="btn btn-sm btn-outline-primary">查看建案完整摘要</RouterLink>
              <button
                v-if="twoASummary.historyStatus !== 'Approved'"
                class="btn btn-sm btn-success"
                type="button"
                :disabled="!twoASummary.publishReady || publishingHistory"
                @click="publishHistory"
              >
                {{ publishingHistory ? '發布中...' : '確認並納入 2A 歷史基準' }}
              </button>
              <span v-else class="badge bg-success align-self-center">已納入歷史基準</span>
            </div>
          </div>
          <div class="row g-2">
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">工程大項辨識</div><div class="stat-num">{{ summary.totalCount - twoASummary.unclassifiedItemCount }}</div><small>/ {{ summary.totalCount }} 筆</small></div></div>
            <div class="col-md-2"><div class="stat-box" :class="{ 'tone-warning': twoASummary.unclassifiedItemCount > 0 }"><div class="text-muted fw-bold">未辨識大項</div><div class="stat-num">{{ twoASummary.unclassifiedItemCount }}</div><small>需校正或忽略</small></div></div>
            <div class="col-md-2"><div class="stat-box" :class="{ 'tone-warning': twoASummary.structuralUnpricedItemCount > 0 }"><div class="text-muted fw-bold">結構體工項</div><div class="stat-num">{{ twoASummary.structuralItemCount }}</div><small>{{ twoASummary.structuralPricedItemCount }} 筆可計價</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">結構體總價</div><div class="stat-num report-amount">{{ formatNumber(twoASummary.structuralAmount) }}</div><small>價格基準候選</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">鋼筋候選</div><div class="stat-num">{{ twoASummary.rebarCandidateItemCount }}</div><small>/ {{ twoASummary.rebarSourceItemCount }} 筆</small></div></div>
            <div class="col-md-2"><div class="stat-box"><div class="text-muted fw-bold">混凝土候選</div><div class="stat-num">{{ twoASummary.concreteCandidateItemCount }}</div><small>/ {{ twoASummary.concreteSourceItemCount }} 筆</small></div></div>
          </div>
          <div
            class="alert mt-3 mb-0 py-2 small"
            :class="twoASummary.historyStatus === 'Approved' || twoASummary.publishReady ? 'alert-success' : 'alert-warning'"
          >
            <template v-if="twoASummary.historyStatus === 'Approved'">
              <strong>已發布：</strong>此建案目前會參與 2A 模型。
            </template>
            <template v-else-if="twoASummary.publishReady">
              <strong>可發布：</strong>目前沒有阻擋歷史基準發布的資料問題。
            </template>
            <template v-else>
              <strong>尚不能發布：</strong>{{ twoASummary.publishBlockingIssues.join('、') }}。
            </template>
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
          <div class="d-flex justify-content-between align-items-start gap-3 mb-2"><h5 class="mb-1">{{ item.itemName }}</h5><div class="d-flex gap-1 flex-wrap justify-content-end"><span v-if="item.effectiveMajorCategory" class="badge bg-info text-dark">{{ item.effectiveMajorCategory }}</span><span v-if="item.contributesStructuralPrice" class="badge bg-success">結構體價格候選</span><span v-if="item.contributesQuantityHistory" class="badge bg-success">{{ item.quantityHistoryCategory }}數量候選</span><span v-if="!item.contributesStructuralPrice && !item.contributesQuantityHistory" class="badge bg-light text-dark border">不納入目前 2A 基準</span><span v-if="item.majorCategoryOverride" class="badge bg-primary">人工大項</span><span class="badge" :class="badge(item).css">{{ badge(item).label }}</span><span v-if="item.invalidQuantity" class="badge bg-danger">數量無效</span><span v-if="item.invalidUnitPrice" class="badge bg-danger">單價無效</span><span v-if="item.unitMismatch" class="badge bg-danger">單位不符</span></div></div>
          <div class="text-muted small mb-2">單位：{{ item.unit || '無' }} ｜ 數量：{{ formatNumber(item.qty, 4) }} ｜ 單價：{{ formatNumber(item.unitPrice) }}</div>
          <div class="text-muted small mb-3">原始分類：{{ [item.category1, item.category2, item.category3].filter(Boolean).join(' → ') || '未辨識' }}</div>
          <div v-if="!item.isIgnored" class="row g-2 mb-3"><div class="col-md-6"><label class="form-label small fw-bold">工程大項</label><select class="form-select" :value="item.majorCategoryOverride ?? ''" :disabled="busyId === item.finalBidItemId" @change="updateMajorCategory(item, $event)"><option value="">使用原始分類（{{ item.effectiveMajorCategory || '未辨識' }}）</option><option v-for="entry in majorCategories" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-md-6 d-flex align-items-end"><div class="small text-muted">人工指定只影響 2A 判定，原始 Category1 / 2 / 3 不會被覆蓋。</div></div></div>
          <div class="d-flex justify-content-between align-items-center gap-2 mb-2 flex-wrap"><div class="small"><strong>目前標準材料：</strong><span v-if="item.materialName">{{ item.materialName }}</span><span v-else class="text-warning">尚未對應</span></div><div class="d-flex gap-2"><button class="btn btn-sm btn-outline-secondary" type="button" @click="openEdit(item)">修正標單資料</button><button class="btn btn-sm btn-outline-primary" type="button" :disabled="busyId === item.finalBidItemId" @click="excludeQuantityHistory(item)">{{ item.isIgnored ? "恢復價格／排除數量歷史" : "僅排除數量歷史" }}</button><button v-if="!item.isIgnored" class="btn btn-sm btn-outline-danger" type="button" @click="ignore(item)">忽略</button></div></div>
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
