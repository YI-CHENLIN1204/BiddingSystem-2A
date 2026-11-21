<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { api, apiForm, queryString } from '../services/api'
import type { ProjectSummary } from '../types'

interface BidItem {
  finalBidItemId: number
  category1?: string | null
  category2?: string | null
  category3?: string | null
  majorCategoryOverride?: string | null
  effectiveMajorCategory?: string | null
  contributesStructuralPrice?: boolean
  contributesQuantityHistory?: boolean
  quantityHistoryCategory?: string | null
  itemName: string
  spec?: string | null
  unit?: string | null
  qty?: number | null
  unitPrice?: number | null
  materialId?: number | null
  materialName?: string | null
  materialCategory?: string | null
  materialUnit?: string | null
  mappingStatus: string
  isIgnored: boolean
  invalidQuantity?: boolean
  invalidUnitPrice?: boolean
  unitMismatch?: boolean
}

interface MaterialOption {
  materialId: number
  materialName: string
  category?: string | null
  unitName: string
}

interface ProjectTwoASummary {
  projectId: number
  historyStatus: string
  historyApprovedAt?: string | null
  historyApprovedBy?: string | null
  majorCategory: string
  structuralItemCount: number
  structuralPricedItemCount: number
  structuralUnpricedItemCount: number
  unclassifiedItemCount: number
  manualMajorCategoryCount: number
  structuralUnmappedItemCount: number
  rebarSourceItemCount: number
  rebarCandidateItemCount: number
  rebarExcludedItemCount: number
  concreteSourceItemCount: number
  concreteCandidateItemCount: number
  concreteExcludedItemCount: number
  publishReady: boolean
  publishBlockingIssues: string[]
  structuralAmount: number
  structuralPricePerPing?: number | null
  rebarQuantity?: number | null
  rebarUnit: string
  rebarTransactionCount: number
  contributesRebarHistory: boolean
  concreteQuantity?: number | null
  concreteUnit: string
  concreteTransactionCount: number
  contributesConcreteHistory: boolean
  otherStructuralItemCount: number
  message: string
}

interface HistoryItem {
  materialId: number
  materialName: string
  category: string
  unitName: string
  transactionCount: number
  totalQuantity: number
}

interface PolicyResponse { knownMajorCategories: string[] }
interface FilterOptions { categories: string[]; units: string[] }
interface PageResult<T> { totalCount: number; page: number; pageSize: number; items: T[] }
interface MasterOption { categoryId?: number; categoryName?: string; unitId?: number; unitName?: string; isActive: boolean }

const route = useRoute()
const projectId = Number(route.params.id)
const project = ref<ProjectSummary | null>(null)
const tab = ref<'basic' | 'bid' | 'history'>('basic')
const bidItems = ref<BidItem[]>([])
const historyItems = ref<HistoryItem[]>([])
const twoASummary = ref<ProjectTwoASummary | null>(null)
const materialOptions = ref<MaterialOption[]>([])
const filterOptions = ref<FilterOptions>({ categories: [], units: [] })
const masterOptions = ref<{ categories: MasterOption[]; units: MasterOption[] }>({ categories: [], units: [] })
const majorCategories = ref<string[]>([])
const selectedCategory = ref<Record<number, string>>({})
const selectedMaterial = ref<Record<number, number | '__NEW__' | null>>({})
const search = ref('')
const status = ref('All')
const majorCategoryFilter = ref('All')
const category = ref('All')
const unit = ref('All')
const issueCode = ref('All')
const page = ref(1)
const pageSize = ref(50)
const totalCount = ref(0)
const loading = ref(false)
const busyId = ref<number | null>(null)
const publishingHistory = ref(false)
const error = ref('')
const message = ref('')
const openMenuId = ref<number | null>(null)
const editingItem = ref<BidItem | null>(null)
const editForm = ref({ itemName: '', unit: '', qty: null as number | null, unitPrice: null as number | null })
const creatingFor = ref<BidItem | null>(null)
const newMaterial = ref({ materialName: '', categoryId: null as number | null, unitId: null as number | null })
let searchTimer: number | undefined

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))
const isHistoryLocked = computed(() => project.value?.historyStatus === 'Approved')

function formatNumber(value: number | null | undefined, digits = 2) {
  if (value == null) return '無'
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: digits }).format(value)
}

function formatMonth(value?: string) {
  return value ? value.slice(0, 7) : '未設定'
}

function materialsFor(item: BidItem) {
  const selected = selectedCategory.value[item.finalBidItemId]
  return materialOptions.value
    .filter(x => (x.category ?? '') === selected)
    .sort((a, b) => a.materialName.localeCompare(b.materialName, 'zh-Hant'))
}

function mappingBadge(item: BidItem) {
  if (item.isIgnored) return { label: '已忽略', className: 'bg-secondary' }
  if (item.mappingStatus === 'Auto') return { label: '自動對應', className: 'bg-success' }
  if (item.mappingStatus === 'Manual') return { label: '人工對應', className: 'bg-primary' }
  return { label: '未對應', className: 'bg-warning text-dark' }
}

async function loadProject() {
  project.value = await api<ProjectSummary>(`/projects/${projectId}`)
}

async function loadFilterOptions() {
  const [filters, materials, masters, policy] = await Promise.all([
    api<FilterOptions>(`/Estimation/mapping-filter-options${queryString({ projectId })}`),
    api<MaterialOption[]>('/Estimation/material-options'),
    api<{ categories: MasterOption[]; units: MasterOption[] }>('/standard-material/filter-options'),
    api<PolicyResponse>('/analysis-cases/policy'),
  ])
  filterOptions.value = filters
  materialOptions.value = materials
  masterOptions.value = masters
  majorCategories.value = policy.knownMajorCategories
}

async function loadBidItems() {
  loading.value = true
  error.value = ''
  try {
    const result = await api<PageResult<BidItem>>(`/Estimation/mapping-items${queryString({
      projectId, status: status.value, search: search.value.trim(), issueCode: issueCode.value,
      majorCategory: majorCategoryFilter.value, category: category.value, unit: unit.value, page: page.value, pageSize: pageSize.value,
    })}`)
    totalCount.value = result.totalCount
    bidItems.value = result.items
    if (page.value > totalPages.value) {
      page.value = totalPages.value
      return await loadBidItems()
    }
    const categories: Record<number, string> = {}
    const materials: Record<number, number | '__NEW__' | null> = {}
    for (const item of result.items) {
      categories[item.finalBidItemId] = item.materialCategory ?? ''
      materials[item.finalBidItemId] = item.materialId ?? null
    }
    selectedCategory.value = categories
    selectedMaterial.value = materials
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '讀取決標工項失敗'
  } finally {
    loading.value = false
  }
}

async function loadHistory() {
  const result = await api<{ totalCount: number; items: HistoryItem[] }>(`/projects/${projectId}/historical-data`)
  historyItems.value = result.items
}

async function loadTwoASummary() {
  twoASummary.value = await api<ProjectTwoASummary>(`/projects/${projectId}/2a-summary`)
}

// Persist a pending operation ID across page reloads. Never create a fresh
// ID to retry a request whose commit outcome remains unknown.
type HistoryAction = 'publish-history' | 'reopen-history-review'
const pendingHistoryKey = (action: HistoryAction) => `history-operation:${projectId}:${action}`

function getOrCreateHistoryOperation(action: HistoryAction): string {
  const key = pendingHistoryKey(action)
  let id = sessionStorage.getItem(key)
  if (!id) {
    id = crypto.randomUUID()
    sessionStorage.setItem(key, id)
  }
  return id
}
function clearHistoryOperation(action: HistoryAction): void {
  sessionStorage.removeItem(pendingHistoryKey(action))
}
async function reconcileHistoryWrite(
  action: HistoryAction, operationId: string, expectedStatus: 'Approved' | 'Draft', failure: unknown,
) {
  const reason = failure instanceof Error ? failure.message : '伺服器連線失敗'
  try {
    // The receipt is committed in the SAME transaction as the history changes.
    // A matching status by itself does not prove which request committed.
    const receipt = await api<{ actionType: string }>(
      `/projects/${projectId}/history-operation/${operationId}`,
    )
    const expectedAction = action === 'publish-history' ? 'PublishHistory' : 'ReopenHistoryReview'
    if (receipt.actionType !== expectedAction) throw new Error('操作收據內容不符')
    clearHistoryOperation(action)
    await Promise.all([loadProject(), loadHistory(), loadTwoASummary(), loadBidItems()])
    message.value = '原始請求已由資料庫操作收據確認成功；未重新執行寫入。'
    return
  } catch {
    // The receipt endpoint can also fail transiently. Preserve the key.
    try {
      await loadProject()
      await Promise.all([loadHistory(), loadTwoASummary(), loadBidItems()])
      error.value = reason +
        `；目前狀態為 ${project.value?.historyStatus ?? '未知'}，但尚未確認這次操作是否提交。已保留原操作識別碼，請勿重新建立新操作。`
    } catch {
      error.value = reason + '；操作結果尚未確認，原操作識別碼已保留，請勿連續重試。'
    }
  }
}

async function runHistoryTransition(
  action: HistoryAction, expectedStatus: 'Approved' | 'Draft', successMessage: string,
) {
  publishingHistory.value = true
  error.value = ''
  message.value = ''
  const operationId = getOrCreateHistoryOperation(action)
  try {
    await api(`/projects/${projectId}/${action}`, {
      method: 'POST', headers: { 'Idempotency-Key': operationId },
    })
    clearHistoryOperation(action)
    try {
      await Promise.all([loadProject(), loadHistory(), loadTwoASummary(), loadBidItems()])
      message.value = successMessage
    } catch (caught) {
      error.value = '資料庫操作已確認成功，但重新載入畫面失敗：' +
        (caught instanceof Error ? caught.message : '未知錯誤')
    }
  } catch (caught) {
    await reconcileHistoryWrite(action, operationId, expectedStatus, caught)
  } finally {
    publishingHistory.value = false
  }
}

async function publishHistory() {
  if (publishingHistory.value || !project.value) return
  if (!window.confirm(
    '確認將此建案納入 2A 歷史基準嗎？\\n\\n系統會依目前工程大項、材料對應、數量與單位建立鋼筋／混凝土歷史交易，並讓本案參與後續相似建案與結構體價格估算。'
  )) return
  await runHistoryTransition('publish-history', 'Approved', '此建案已確認並納入 2A 歷史基準。')
}

async function reopenHistoryReview() {
  if (publishingHistory.value || !project.value) return
  if (!window.confirm(
    '確定重新開啟審核嗎？\\n\\n此建案會暫時退出 2A 模型，既有數量歷史交易會停用，修正完成後可再次發布。'
  )) return
  await runHistoryTransition('reopen-history-review', 'Draft', '已重新開啟歷史基準審核。')
}

async function reloadRelated(messageText?: string) {
  await Promise.all([loadBidItems(), loadHistory(), loadProject(), loadTwoASummary()])
  if (messageText) message.value = messageText
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
    await reloadRelated(value ? '工程大項已人工確認。' : '已恢復使用標單原始分類。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '工程大項更新失敗'
  } finally {
    busyId.value = null
  }
}

function changeCategory(item: BidItem) {
  const candidates = materialsFor(item)
  if (!candidates.some(x => x.materialId === selectedMaterial.value[item.finalBidItemId])) {
    selectedMaterial.value[item.finalBidItemId] = null
  }
}

async function updateMapping(item: BidItem) {
  const materialId = selectedMaterial.value[item.finalBidItemId]
  if (materialId === '__NEW__') {
    openCreate(item)
    return
  }
  if (!materialId) {
    error.value = '請先選擇標準材料。'
    return
  }
  busyId.value = item.finalBidItemId
  error.value = ''
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    form.append('materialId', String(materialId))
    await apiForm('/Estimation/update-material-mapping', form)
    await reloadRelated('材料對應已更新。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '材料對應失敗'
  } finally {
    busyId.value = null
  }
}

async function excludeQuantityHistory(item: BidItem) {
  openMenuId.value = null
  if (!window.confirm(`確定保留「${item.itemName}」的工程價格，僅排除鋼筋／混凝土數量歷史嗎？`)) return
  busyId.value = item.finalBidItemId
  error.value = ''
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    await apiForm('/Estimation/exclude-quantity-history', form)
    await reloadRelated('已保留工程價格，並排除材料數量歷史。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '排除數量歷史失敗'
  } finally {
    busyId.value = null
  }
}

async function setIgnored(item: BidItem, ignored: boolean) {
  openMenuId.value = null
  const prompt = ignored
    ? `確定要忽略「${item.itemName}」嗎？\n\n忽略後，此工項不會進入模型歷史資料。`
    : `確定要取消忽略「${item.itemName}」嗎？`
  if (!window.confirm(prompt)) return
  busyId.value = item.finalBidItemId
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(item.finalBidItemId))
    await apiForm(`/Estimation/${ignored ? 'ignore-bid-item' : 'unignore-bid-item'}`, form)
    await reloadRelated(ignored ? '工項已忽略。' : '已取消忽略。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '更新工項失敗'
  } finally {
    busyId.value = null
  }
}

function openEdit(item: BidItem) {
  openMenuId.value = null
  editingItem.value = item
  editForm.value = {
    itemName: item.itemName,
    unit: item.unit ?? '',
    qty: item.qty ?? null,
    unitPrice: item.unitPrice ?? null,
  }
}

async function saveItem() {
  if (!editingItem.value) return
  busyId.value = editingItem.value.finalBidItemId
  try {
    const form = new FormData()
    form.append('finalBidItemId', String(editingItem.value.finalBidItemId))
    form.append('itemName', editForm.value.itemName)
    form.append('unit', editForm.value.unit)
    form.append('qty', editForm.value.qty == null ? '' : String(editForm.value.qty))
    form.append('unitPrice', editForm.value.unitPrice == null ? '' : String(editForm.value.unitPrice))
    await apiForm('/Estimation/update-bid-item-data', form)
    editingItem.value = null
    await reloadRelated('工項資料已更新。')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '工項更新失敗'
  } finally {
    busyId.value = null
  }
}

function openCreate(item: BidItem) {
  openMenuId.value = null
  creatingFor.value = item
  const selected = masterOptions.value.categories.find(x => x.categoryName === selectedCategory.value[item.finalBidItemId])
  newMaterial.value = { materialName: '', categoryId: selected?.categoryId ?? null, unitId: null }
}

async function createMaterial() {
  const target = creatingFor.value
  const materialName = newMaterial.value.materialName.trim()
  if (!target || !materialName || !newMaterial.value.categoryId || !newMaterial.value.unitId) {
    error.value = '請完整填寫材料名稱、分類與單位。'
    return
  }

  busyId.value = target.finalBidItemId
  error.value = ''
  try {
    const form = new FormData()
    form.append('materialName', materialName)
    form.append('categoryId', String(newMaterial.value.categoryId))
    form.append('unitId', String(newMaterial.value.unitId))
    const created = await apiForm<{ materialId?: number; categoryName?: string }>('/standard-material/create', form)
    await loadFilterOptions()
    selectedCategory.value[target.finalBidItemId] = created.categoryName
      ?? masterOptions.value.categories.find(x => x.categoryId === newMaterial.value.categoryId)?.categoryName
      ?? ''
    selectedMaterial.value[target.finalBidItemId] = created.materialId ?? null
    creatingFor.value = null
    message.value = '標準材料已新增，請確認後執行人工對應。'
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '新增標準材料失敗'
  } finally {
    busyId.value = null
  }
}

function resetAndLoad() {
  page.value = 1
  loadBidItems()
}

watch(search, () => {
  window.clearTimeout(searchTimer)
  searchTimer = window.setTimeout(resetAndLoad, 300)
})
watch([status, majorCategoryFilter, category, unit, issueCode, pageSize], resetAndLoad)

onMounted(async () => {
  if (!Number.isInteger(projectId) || projectId <= 0) {
    error.value = '無效的建案編號。'
    return
  }
  try {
    await Promise.all([loadProject(), loadFilterOptions(), loadHistory(), loadTwoASummary()])
    await loadBidItems()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入建案資料'
  }
})
</script>

<template>
  <div>
    <div class="mb-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
      <RouterLink class="text-decoration-none" to="/projects">← 返回建案管理</RouterLink>
      <RouterLink v-if="project?.hasFinalBid" class="btn btn-outline-primary btn-sm" :to="{ path: '/final-bid', query: { projectId: String(projectId) } }">開啟決標工項審核</RouterLink>
    </div>
    <div v-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-if="message" class="alert alert-success alert-dismissible">
      {{ message }}<button type="button" class="btn-close" @click="message = ''"></button>
    </div>

    <div class="card p-4 mb-3">
      <div v-if="!project" class="text-muted">載入建案資料中...</div>
      <div v-else class="d-flex justify-content-between align-items-start gap-3 flex-wrap">
        <div>
          <div class="d-flex align-items-center gap-2 flex-wrap">
            <h3 class="mb-1">{{ project.projectName }}</h3>
            <span class="badge" :class="project.hasFinalBid ? 'bg-secondary' : 'bg-light text-dark border'">
              {{ project.hasFinalBid ? '已有決標資料' : '尚無決標資料' }}
            </span>
            <span
              v-if="project.hasFinalBid"
              class="badge"
              :class="project.historyStatus === 'Approved' ? 'bg-success' : 'bg-warning text-dark'"
            >
              {{ project.historyStatus === 'Approved' ? '已納入 2A 模型' : '待歷史基準審核' }}
            </span>
          </div>
          <div class="text-muted">{{ project.projectType }} ・ {{ project.structureType }} ・ {{ project.city }} ・ {{ formatMonth(project.startDate) }}</div>
        </div>
        <div class="text-end">
          <div class="small text-muted mb-2">
            決標工項 {{ project.finalBidItemCount ?? 0 }} 筆<br />
            有效歷史交易 {{ project.historicalTransactionCount ?? 0 }} 筆
          </div>
          <button
            v-if="project.hasFinalBid && project.historyStatus !== 'Approved'"
            class="btn btn-sm btn-success"
            type="button"
            :disabled="publishingHistory || (twoASummary != null && !twoASummary.publishReady)"
            @click="publishHistory"
          >
            {{ publishingHistory ? '處理中...' : '確認並納入 2A 歷史基準' }}
          </button>
          <button
            v-else-if="project.hasFinalBid"
            class="btn btn-sm btn-outline-warning"
            type="button"
            :disabled="publishingHistory"
            @click="reopenHistoryReview"
          >
            {{ publishingHistory ? '處理中...' : '重新開啟審核' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="isHistoryLocked" class="alert alert-success mb-3">
      此建案目前是已發布的 2A 歷史基準。為避免已驗證歷史資料被直接改動，建案條件、工程大項、材料對應、數量、單價與忽略狀態目前鎖定；如需修正，請先按「重新開啟審核」。
    </div>

    <ul class="nav nav-tabs mb-3">
      <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'basic' }" @click="tab = 'basic'">基本資料</button></li>
      <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'bid' }" @click="tab = 'bid'">決標工項</button></li>
      <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'history' }" @click="tab = 'history'">模型歷史資料</button></li>
    </ul>

    <section v-if="tab === 'basic' && project" class="basic-stack">
      <div class="card p-4">
        <div class="row g-4">
          <div class="col-md-3"><div class="text-muted small">建案名稱</div><strong>{{ project.projectName }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">建案類型</div><strong>{{ project.projectType || '未設定' }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">結構類型</div><strong>{{ project.structureType || '未設定' }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">縣市</div><strong>{{ project.city || '未設定' }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">總樓地板面積</div><strong>{{ formatNumber(project.totalFloorArea) }} m²</strong></div>
          <div class="col-md-3"><div class="text-muted small">基地面積</div><strong>{{ formatNumber(project.siteArea) }} m²</strong></div>
          <div class="col-md-3"><div class="text-muted small">地上樓層</div><strong>{{ project.floorCount ?? '未設定' }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">地下樓層</div><strong>{{ project.basementFloorCount ?? '未設定' }}</strong></div>
          <div class="col-md-3"><div class="text-muted small">建案年月</div><strong>{{ formatMonth(project.startDate) }}</strong></div>
        </div>
      </div>

      <div v-if="twoASummary" class="card baseline-card">
        <div class="baseline-header">
          <div>
            <h5>2A 歷史基準摘要</h5>
            <div class="text-muted small">
              本建案定案後，實際能提供給 2A 的「結構體工程價格」與「鋼筋／混凝土數量」資料。
            </div>
          </div>
          <span class="badge bg-light text-dark border">{{ twoASummary.majorCategory }}</span>
        </div>

        <div
          class="baseline-status"
          :class="twoASummary.historyStatus === 'Approved' ? 'approved' : twoASummary.publishReady ? 'ready' : 'draft'"
        >
          <strong>
            {{ twoASummary.historyStatus === 'Approved'
              ? '已發布歷史基準'
              : twoASummary.publishReady
                ? '審核資料已符合發布條件'
                : '目前為審核中' }}
          </strong>
          <span v-if="twoASummary.historyStatus === 'Approved'">
            此建案會參與相似建案篩選、結構體價格估算與鋼筋／混凝土數量基準。
          </span>
          <span v-else-if="twoASummary.publishReady">
            工程大項、結構體價格與鋼筋／混凝土候選資料目前沒有阻擋問題，可人工確認後發布。
          </span>
          <span v-else>
            發布前還需要處理：{{ twoASummary.publishBlockingIssues.join('、') }}。
          </span>
        </div>

        <div class="baseline-grid">
          <div class="baseline-cell">
            <span>結構體工程總價</span>
            <strong>{{ formatNumber(twoASummary.structuralAmount) }}</strong>
            <small>
              {{ twoASummary.structuralPricedItemCount }} / {{ twoASummary.structuralItemCount }} 筆可計價
            </small>
          </div>
          <div class="baseline-cell">
            <span>結構體工程每坪單價</span>
            <strong>{{ formatNumber(twoASummary.structuralPricePerPing) }}</strong>
            <small>作為大項價格歷史基準</small>
          </div>
          <div class="baseline-cell">
            <span>鋼筋歷史總量</span>
            <strong>{{ formatNumber(twoASummary.rebarQuantity, 4) }} {{ twoASummary.rebarUnit }}</strong>
            <small :class="twoASummary.contributesRebarHistory ? 'text-success' : 'text-muted'">
              {{ twoASummary.contributesRebarHistory ? '已貢獻 ' + twoASummary.rebarTransactionCount + ' 筆歷史交易' : '尚未形成有效歷史資料' }}
            </small>
          </div>
          <div class="baseline-cell">
            <span>混凝土歷史總量</span>
            <strong>{{ formatNumber(twoASummary.concreteQuantity, 4) }} {{ twoASummary.concreteUnit }}</strong>
            <small :class="twoASummary.contributesConcreteHistory ? 'text-success' : 'text-muted'">
              {{ twoASummary.contributesConcreteHistory ? '已貢獻 ' + twoASummary.concreteTransactionCount + ' 筆歷史交易' : '尚未形成有效歷史資料' }}
            </small>
          </div>
        </div>

        <div class="qualification-grid">
          <div>
            <span>未辨識工程大項</span>
            <strong :class="twoASummary.unclassifiedItemCount ? 'text-warning' : 'text-success'">{{ twoASummary.unclassifiedItemCount }}</strong>
            <small>可到「決標工項」人工指定，不修改原始分類欄位。</small>
          </div>
          <div>
            <span>人工校正大項</span>
            <strong>{{ twoASummary.manualMajorCategoryCount }}</strong>
            <small>已保留原始 Category1 / 2 / 3，可隨時恢復。</small>
          </div>
          <div>
            <span>結構體未對應材料</span>
            <strong :class="twoASummary.structuralUnmappedItemCount ? 'text-warning' : 'text-success'">{{ twoASummary.structuralUnmappedItemCount }}</strong>
            <small>只影響鋼筋／混凝土數量歷史，不影響已存在的結構體價格。</small>
          </div>
          <div>
            <span>結構體無可用價格</span>
            <strong :class="twoASummary.structuralUnpricedItemCount ? 'text-warning' : 'text-success'">{{ twoASummary.structuralUnpricedItemCount }}</strong>
            <small>Amount 或 Qty × UnitPrice 都無法形成價格時才會列入。</small>
          </div>
          <div>
            <span>鋼筋數量資格</span>
            <strong>{{ twoASummary.rebarCandidateItemCount }} / {{ twoASummary.rebarSourceItemCount }}</strong>
            <small>{{ twoASummary.rebarExcludedItemCount }} 筆因數量／單位等條件未通過候選資格。</small>
          </div>
          <div>
            <span>混凝土數量資格</span>
            <strong>{{ twoASummary.concreteCandidateItemCount }} / {{ twoASummary.concreteSourceItemCount }}</strong>
            <small>{{ twoASummary.concreteExcludedItemCount }} 筆因數量／單位等條件未通過候選資格。</small>
          </div>
        </div>

        <div class="baseline-foot">
          <span>{{ twoASummary.message }}</span>
          <span v-if="twoASummary.otherStructuralItemCount > 0">
            另有 {{ twoASummary.otherStructuralItemCount }} 筆結構體工項保留於最終標單，但不作為鋼筋／混凝土數量基準。
          </span>
        </div>
      </div>
    </section>

    <section v-else-if="tab === 'bid'">
      <div class="card p-3 mb-3">
        <div class="row g-2">
          <div class="col-lg-4"><input v-model="search" type="search" class="form-control" placeholder="搜尋工項名稱或標準材料" /></div>
          <div class="col-md-2"><select v-model="status" class="form-select"><option value="All">全部對應狀態</option><option value="Pending">未對應</option><option value="Auto">自動對應</option><option value="Manual">人工對應</option><option value="Ignored">已忽略</option></select></div>
          <div class="col-md-3"><select v-model="majorCategoryFilter" class="form-select"><option value="All">全部工程大項</option><option value="Unclassified">未辨識工程大項</option><option v-for="item in majorCategories" :key="item" :value="item">{{ item }}</option></select></div>
          <div class="col-md-3"><select v-model="category" class="form-select"><option value="All">全部材料分類</option><option v-for="item in filterOptions.categories" :key="item" :value="item">{{ item }}</option></select></div>
          <div class="col-md-3"><select v-model="unit" class="form-select"><option value="All">全部單位</option><option v-for="item in filterOptions.units" :key="item" :value="item">{{ item }}</option></select></div>
          <div class="col-md-3"><select v-model="issueCode" class="form-select"><option value="All">全部資料狀態</option><option value="INVALID_QUANTITY">數量無效</option><option value="INVALID_UNIT_PRICE">單價無效</option><option value="UNIT_MISMATCH">單位不符</option></select></div>
        </div>
        <div class="d-flex justify-content-between align-items-center mt-3">
          <div class="text-muted small">共 {{ totalCount }} 筆</div>
          <div class="d-flex align-items-center gap-2"><span class="text-muted small">每頁</span><select v-model="pageSize" class="form-select form-select-sm page-size"><option :value="20">20</option><option :value="50">50</option><option :value="100">100</option></select></div>
        </div>
      </div>

      <div v-if="loading" class="card p-4 text-muted">載入決標工項中...</div>
      <div v-else-if="!bidItems.length" class="card p-4 text-muted">沒有符合條件的工項</div>
      <article v-for="item in bidItems" v-else :key="item.finalBidItemId" class="card mb-3 bid-card">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-start mb-2 gap-3">
            <h5 class="mb-1">{{ item.itemName }}</h5>
            <div class="d-flex align-items-center gap-1 flex-wrap justify-content-end">
              <span v-if="item.effectiveMajorCategory" class="badge bg-info text-dark">{{ item.effectiveMajorCategory }}</span>
              <span v-if="item.contributesStructuralPrice" class="badge bg-success">{{ project?.historyStatus === 'Approved' ? '結構體價格基準' : '結構體價格候選' }}</span>
              <span v-if="item.contributesQuantityHistory" class="badge bg-success">{{ item.quantityHistoryCategory }}{{ project?.historyStatus === 'Approved' ? '數量基準' : '數量候選' }}</span>
              <span v-if="!item.contributesStructuralPrice && !item.contributesQuantityHistory" class="badge bg-light text-dark border">不納入目前 2A 基準</span>
              <span v-if="item.majorCategoryOverride" class="badge bg-primary">人工大項</span>
              <span class="badge" :class="mappingBadge(item).className">{{ mappingBadge(item).label }}</span>
              <span v-if="item.invalidQuantity" class="badge bg-danger">數量無效</span>
              <span v-if="item.invalidUnitPrice" class="badge bg-danger">單價無效</span>
              <span v-if="item.unitMismatch" class="badge bg-danger">單位不符</span>
              <div class="item-menu">
                <button class="btn btn-sm btn-light border" type="button" :disabled="isHistoryLocked" @click="openMenuId = openMenuId === item.finalBidItemId ? null : item.finalBidItemId">⋯</button>
                <div v-if="openMenuId === item.finalBidItemId" class="dropdown-menu dropdown-menu-end show">
                  <button class="dropdown-item" type="button" @click="openEdit(item)">編輯工項</button>
                  <button v-if="!isHistoryLocked" class="dropdown-item text-primary" type="button" :disabled="busyId === item.finalBidItemId" @click="excludeQuantityHistory(item)">{{ item.isIgnored ? "恢復價格／排除數量歷史" : "僅排除數量歷史" }}</button>
                  <button v-if="item.isIgnored" class="dropdown-item" type="button" @click="setIgnored(item, false)">取消忽略</button>
                  <button v-else class="dropdown-item text-danger" type="button" @click="setIgnored(item, true)">忽略此工項</button>
                </div>
              </div>
            </div>
          </div>
          <div class="text-muted small mb-2">單位：{{ item.unit || '無' }} ｜ 數量：{{ formatNumber(item.qty, 4) }} ｜ 單價：{{ formatNumber(item.unitPrice) }}</div>
          <div class="text-muted small mb-3">
            原始分類：{{ [item.category1, item.category2, item.category3].filter(Boolean).join(' → ') || '未辨識' }}
          </div>
          <div v-if="!item.isIgnored" class="row g-2 mb-3">
            <div class="col-md-6">
              <label class="form-label small fw-bold">工程大項</label>
              <select
                class="form-select"
                :value="item.majorCategoryOverride ?? ''"
                :disabled="isHistoryLocked || busyId === item.finalBidItemId"
                @change="updateMajorCategory(item, $event)"
              >
                <option value="">使用原始分類（{{ item.effectiveMajorCategory || '未辨識' }}）</option>
                <option v-for="entry in majorCategories" :key="entry" :value="entry">{{ entry }}</option>
              </select>
            </div>
            <div class="col-md-6 d-flex align-items-end">
              <div class="small text-muted">
                人工指定只覆蓋 2A 判定用大項，原始 Category1 / 2 / 3 仍完整保留。
              </div>
            </div>
          </div>
          <div class="mb-2 small"><strong>目前標準材料：</strong><span v-if="item.materialName">{{ item.materialName }}</span><span v-else class="text-warning">尚未對應</span></div>
          <div v-if="!item.isIgnored" class="row g-2">
            <div class="col-md-4"><label class="form-label small fw-bold">材料分類</label><select v-model="selectedCategory[item.finalBidItemId]" class="form-select" :disabled="isHistoryLocked" @change="changeCategory(item)"><option value="">請選擇材料分類</option><option v-for="entry in filterOptions.categories" :key="entry" :value="entry">{{ entry }}</option></select></div>
            <div class="col-md-5"><label class="form-label small fw-bold">標準材料</label><select v-model="selectedMaterial[item.finalBidItemId]" class="form-select" :disabled="isHistoryLocked || !selectedCategory[item.finalBidItemId]"><option :value="null">請選擇標準材料</option><option v-for="material in materialsFor(item)" :key="material.materialId" :value="material.materialId">{{ material.materialName }} ({{ material.unitName }})</option><option value="__NEW__">＋ 新增標準材料</option></select></div>
            <div class="col-md-3"><label class="form-label small fw-bold">&nbsp;</label><button class="btn btn-outline-primary w-100" type="button" :disabled="isHistoryLocked || busyId === item.finalBidItemId" @click="updateMapping(item)">{{ selectedMaterial[item.finalBidItemId] === '__NEW__' ? '新增標準材料' : item.mappingStatus === 'Pending' ? '人工對應' : '修改對應' }}</button></div>
          </div>
        </div>
      </article>

      <div class="d-flex justify-content-between align-items-center mt-3">
        <button class="btn btn-outline-secondary btn-sm" :disabled="page <= 1" @click="page--; loadBidItems()">上一頁</button>
        <span class="text-muted">第 {{ page }} / {{ totalPages }} 頁</span>
        <button class="btn btn-outline-secondary btn-sm" :disabled="page >= totalPages" @click="page++; loadBidItems()">下一頁</button>
      </div>
    </section>

    <section v-else class="card">
      <div v-if="!historyItems.length" class="p-4 text-muted">目前沒有可供模型使用的歷史資料</div>
      <div v-else class="table-responsive">
        <table class="table table-hover mb-0">
          <thead><tr><th>材料分類</th><th>偵錯材料</th><th>來源工項筆數</th><th>標準化歷史總量</th></tr></thead>
          <tbody><tr v-for="item in historyItems" :key="item.materialId"><td>{{ item.category }}</td><td>{{ item.materialName }}</td><td>{{ item.transactionCount }}</td><td>{{ formatNumber(item.totalQuantity, 4) }} {{ item.unitName }}</td></tr></tbody>
        </table>
      </div>
    </section>

    <div v-if="editingItem" class="modal-backdrop-custom" @mousedown.self="editingItem = null">
      <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="edit-bid-title">
        <form class="modal-content" @submit.prevent="saveItem">
          <div class="modal-header"><h5 id="edit-bid-title" class="modal-title">編輯工項</h5><button type="button" class="btn-close" @click="editingItem = null"></button></div>
          <div class="modal-body">
            <div class="mb-3"><label class="form-label">工項名稱</label><input v-model="editForm.itemName" class="form-control" required /></div>
            <div class="mb-3"><label class="form-label">單位</label><select v-model="editForm.unit" class="form-select"><option value="">請選擇單位</option><option v-for="entry in filterOptions.units" :key="entry" :value="entry">{{ entry }}</option></select></div>
            <div class="row g-3"><div class="col-md-6"><label class="form-label">數量</label><input v-model.number="editForm.qty" type="number" step="any" class="form-control" /></div><div class="col-md-6"><label class="form-label">單價</label><input v-model.number="editForm.unitPrice" type="number" step="any" class="form-control" /></div></div>
          </div>
          <div class="modal-footer"><button type="button" class="btn btn-secondary" @click="editingItem = null">取消</button><button type="submit" class="btn btn-primary" :disabled="busyId != null">儲存</button></div>
        </form>
      </div>
    </div>

    <div v-if="creatingFor" class="modal-backdrop-custom" @mousedown.self="creatingFor = null">
      <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="create-material-title">
        <form class="modal-content" @submit.prevent="createMaterial">
          <div class="modal-header"><h5 id="create-material-title" class="modal-title">新增標準材料</h5><button type="button" class="btn-close" aria-label="關閉" @click="creatingFor = null"></button></div>
          <div class="modal-body">
            <div class="mb-3"><label class="form-label">材料名稱</label><input v-model="newMaterial.materialName" class="form-control" required /></div>
            <div class="mb-3"><label class="form-label">材料分類</label><select v-model="newMaterial.categoryId" class="form-select" required><option :value="null">請選擇材料分類</option><option v-for="entry in masterOptions.categories.filter(x => x.isActive)" :key="entry.categoryId" :value="entry.categoryId">{{ entry.categoryName }}</option></select></div>
            <div class="mb-3"><label class="form-label">標準單位</label><select v-model="newMaterial.unitId" class="form-select" required><option :value="null">請選擇標準單位</option><option v-for="entry in masterOptions.units.filter(x => x.isActive)" :key="entry.unitId" :value="entry.unitId">{{ entry.unitName }}</option></select></div>
          </div>
          <div class="modal-footer"><button type="button" class="btn btn-secondary" @click="creatingFor = null">取消</button><button type="submit" class="btn btn-primary" :disabled="busyId != null">新增</button></div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-size { width: 90px; }
.bid-card { overflow: visible; }
.item-menu { position: relative; }
.item-menu .dropdown-menu { position: absolute; top: 100%; right: 0; left: auto; z-index: 20; }
.modal-backdrop-custom { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; background: rgba(0,0,0,.5); }
.modal-backdrop-custom .modal-dialog { width: min(500px, 100%); margin: auto; }
.basic-stack{display:grid;gap:16px}.baseline-card{overflow:hidden}.baseline-status{display:flex;gap:10px;align-items:center;padding:11px 18px;border-bottom:1px solid #edf0f4;font-size:12px}.baseline-status span{color:#5f6b7a}.baseline-status.approved,.baseline-status.ready{background:#eefaf4;color:#176e50}.baseline-status.draft{background:#fff8e3;color:#8a6511}.baseline-header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;padding:16px 18px;background:#f8f9fa;border-bottom:1px solid #e3e5e8}.baseline-header h5{margin:0 0 4px;font-size:15px;font-weight:800;color:#321fdb}.baseline-grid{display:grid;grid-template-columns:repeat(4,1fr)}.baseline-cell{padding:18px;border-right:1px solid #edf0f4}.baseline-cell:last-child{border-right:0}.baseline-cell>span{display:block;color:#6c757d;font-size:12px}.baseline-cell>strong{display:block;margin-top:7px;font-size:20px;font-variant-numeric:tabular-nums}.baseline-cell>small{display:block;margin-top:6px;font-size:11px}.qualification-grid{display:grid;grid-template-columns:repeat(3,1fr);border-top:1px solid #edf0f4}.qualification-grid>div{padding:14px 18px;border-right:1px solid #edf0f4;border-bottom:1px solid #edf0f4}.qualification-grid>div:nth-child(3n){border-right:0}.qualification-grid>div:nth-last-child(-n+3){border-bottom:0}.qualification-grid span,.qualification-grid small{display:block}.qualification-grid span{color:#6c757d;font-size:11px}.qualification-grid strong{display:block;margin:5px 0;font-size:18px}.qualification-grid small{color:#6c757d;font-size:10px;line-height:1.4}.baseline-foot{display:flex;justify-content:space-between;gap:18px;padding:11px 18px;background:#fbfcfd;border-top:1px solid #edf0f4;color:#6c757d;font-size:11px}@media(max-width:1000px){.qualification-grid{grid-template-columns:repeat(2,1fr)}.qualification-grid>div{border-right:1px solid #edf0f4!important;border-bottom:1px solid #edf0f4!important}.qualification-grid>div:nth-child(2n){border-right:0!important}.qualification-grid>div:nth-last-child(-n+2){border-bottom:0!important}.baseline-grid{grid-template-columns:repeat(2,1fr)}.baseline-cell:nth-child(2){border-right:0}.baseline-cell:nth-child(-n+2){border-bottom:1px solid #edf0f4}.baseline-foot{flex-direction:column}}@media(max-width:600px){.qualification-grid{grid-template-columns:1fr}.qualification-grid>div{border-right:0!important;border-bottom:1px solid #edf0f4!important}.qualification-grid>div:last-child{border-bottom:0!important}.baseline-grid{grid-template-columns:1fr}.baseline-cell{border-right:0;border-bottom:1px solid #edf0f4}.baseline-cell:last-child{border-bottom:0}}
</style>
