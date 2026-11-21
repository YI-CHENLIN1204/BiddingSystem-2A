<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { api, apiForm } from '../services/api'
import WorkbookSheetPicker from '../components/WorkbookSheetPicker.vue'
import MaterialCapabilityPanel from '../components/MaterialCapabilityPanel.vue'
import BulkMaterialEstimatePanel from '../components/BulkMaterialEstimatePanel.vue'

interface CaseSummary {
  analysisCaseId: number
  caseName: string
  projectType: string
  structureType: string
  city: string
  totalFloorArea: number
  siteArea: number
  floorCount: number
  basementFloorCount: number
  projectDate: string
  status: string
  versionCount: number
  currentVersionId?: number | null
  currentVersionNo?: number | null
  currentSourceType?: string | null
  updatedAt: string
}

interface VersionSummary {
  analysisBidVersionId: number
  versionNo: number
  sourceType: string
  sourceOneDProjectId?: number | null
  sourceFileName?: string | null
  sourceNote?: string | null
  isCurrent: boolean
  itemCount: number
  createdAt: string
}

interface CaseDetail {
  case: CaseSummary
  versions: VersionSummary[]
}

interface BidItem {
  analysisBidItemId: number
  sourceSequence?: number | null
  sheetName?: string | null
  rowNumber?: number | null
  itemCode?: string | null
  sourceCategory1?: string | null
  sourceCategory2?: string | null
  sourceCategory3?: string | null
  majorCategory?: string | null
  itemName: string
  spec?: string | null
  unit?: string | null
  quantity?: number | null
  unitPrice?: number | null
  amount?: number | null
  sourceConfidence?: number | null
  note?: string | null
}

interface VersionDetail {
  case: CaseSummary
  version: VersionSummary
  items: BidItem[]
}

interface QuantityScopeItem {
  analysisBidItemId: number
  sheetName?: string | null
  rowNumber?: number | null
  itemName: string
  spec?: string | null
  unit?: string | null
  quantity?: number | null
  majorCategory?: string | null
  detectionCategory?: string | null
  status: 'Included' | 'Excluded' | 'Pending'
  reason: string
  candidateType: 'Rebar' | 'Concrete' | 'Excluded' | 'Other'
  unitNormalizationStatus?: string | null
  normalizedQuantity?: number | null
  normalizedUnit?: string | null
}

interface QuantityScope {
  analysisCaseId: number
  analysisBidVersionId: number
  itemCount: number
  includedItemCount: number
  excludedItemCount: number
  pendingItemCount: number
  excludedQuantityCandidateCount: number
  items: QuantityScopeItem[]
  note: string
}

interface RunSummary {
  analysisRunId: number
  analysisBidVersionId: number
  versionNo: number
  runType: string
  status: string
  createdAt: string
}

interface QuantityReference {
  projectId: number
  projectName: string
  projectDate: string
  floorArea: number
  actualQuantity: number
  usagePerArea: number
  similarityScore: number
  ageYears: number
  similarityWeight: number
  timeWeight: number
  finalWeight: number
}

interface QuantityRange {
  effectiveSampleSize?: number
  largestWeightSharePercent?: number
  references?: QuantityReference[]
  lowerQuantity?: number | null
  medianQuantity?: number | null
  upperQuantity?: number | null
  lowerUsagePerArea?: number | null
  medianUsagePerArea?: number | null
  upperUsagePerArea?: number | null
  referenceProjectCount: number
  statusMessage: string
}

interface QuantityAnalysisItem {
  majorCategoryName: string
  detectionCategoryName: string
  observedQuantity?: number | null
  unitName?: string | null
  observedUsagePerArea?: number | null
  analysisStatus: string
  trafficLight: string
  message: string
  range?: QuantityRange | null
}

interface QuantityAnalysis {
  analysisCaseId: number
  totalGroupCount: number
  analyzedGroupCount: number
  insufficientDataCount: number
  skippedGroupCount: number
  items: QuantityAnalysisItem[]
}

interface QuantityRunResponse {
  analysisRunId: number
  analysisCaseId: number
  analysisBidVersionId: number
  versionNo: number
  sourceType: string
  result: QuantityAnalysis
}

interface PriceAnalysis {
  effectiveSampleSize?: number
  largestWeightSharePercent?: number
  majorCategory: string
  candidateProjectCount: number
  availableProjectCount: number
  referenceProjectCount: number
  hasEnoughSamples: boolean
  statusMessage: string
  lowerPricePerPing?: number | null
  medianPricePerPing?: number | null
  upperPricePerPing?: number | null
  lowerAmount?: number | null
  medianAmount?: number | null
  upperAmount?: number | null
  observedAmount?: number | null
  observedPricePerPing?: number | null
  analysisStatus: string
  trafficLight: string
}

interface PriceRunResponse {
  analysisRunId: number
  result: PriceAnalysis
}

interface OneDStatus {
  configured: boolean
  message: string
}

interface AnalysisPolicy {
  quantityEstimation: {
    estimableMajorCategories: string[]
    detectionCategories: string[]
    rule: string
  }
  priceEstimation: {
    majorCategories: string[]
    rule: string
  }
}

interface AnalysisRunDetail {
  analysisRunId: number
  analysisBidVersionId: number
  versionNo: number
  runType: string
  status: string
  createdAt: string
  result: QuantityAnalysis | PriceAnalysis
}

const route = useRoute()
const caseId = Number(route.params.id)
const detail = ref<CaseDetail | null>(null)
const versionDetail = ref<VersionDetail | null>(null)
const quantityScope = ref<QuantityScope | null>(null)
const quantityScopeError = ref('')
const scopeReviewRows = computed(() =>
  (quantityScope.value?.items ?? []).filter(item => item.status !== 'Included'),
)
const runs = ref<RunSummary[]>([])
const selectedVersionId = ref<number | null>(null)
const loading = ref(false)
const versionLoading = ref(false)
const analyzing = ref(false)
const error = ref('')
const message = ref('')
const tab = ref<'items' | 'analysis' | 'runs'>('items')

const showAddVersion = ref(false)
const versionSource = ref<'OneD' | 'Upload'>('Upload')
const oneDStatus = ref<OneDStatus>({ configured: false, message: '1D API 尚未設定' })
const policy = ref<AnalysisPolicy | null>(null)
const oneDProjectId = ref<number | null>(null)
const versionFile = ref<File | null>(null)
const includedSheets = ref<string[]>([])
const workbookReady = ref(false)
const versionFileInput = ref<HTMLInputElement | null>(null)
const versionNote = ref('')
const versionSaving = ref(false)

const quantityResult = ref<QuantityRunResponse | null>(null)
const priceResult = ref<PriceRunResponse | null>(null)
const selectedMajorCategory = ref('')
const topCount = ref<number | null>(null)

const currentVersion = computed(() =>
  detail.value?.versions.find(x => x.isCurrent) ?? null,
)

const majorCategories = computed(() => {
  const allowed = new Set(
    policy.value?.priceEstimation.majorCategories ?? ['結構體工程'],
  )

  const values = new Set(
    (versionDetail.value?.items ?? [])
      .map(x => x.majorCategory?.trim())
      .filter((x): x is string => Boolean(x) && allowed.has(x)),
  )

  return [...values].sort((a, b) => a.localeCompare(b, 'zh-Hant'))
})

function isRecognizableDemoName(name: string | null | undefined) {
  return /DEMO|模擬|測試資料|測試案件|測試建案/i.test(name ?? '')
}

function formatNumber(value: number | null | undefined, digits = 2, empty = '-') {
  if (value == null || !Number.isFinite(Number(value))) return empty
  return Number(value).toLocaleString('zh-TW', { maximumFractionDigits: digits })
}

function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleString('zh-TW') : '-'
}

function sourceLabel(value: string) {
  return value === 'OneD' ? '1D 匯入' : value === 'Upload' ? '自行上傳' : value
}

function statusLabel(value: string) {
  return ({
    Normal: '正常',
    High: '偏高',
    Low: '偏低',
    InsufficientData: '樣本不足',
    DetectionCategoryNotConfigured: '尚未設定',
    ObservedQuantityUnavailable: '資料不足',
    UnitMismatch: '單位不符',
  } as Record<string, string>)[value] ?? value
}

function trafficClass(value: string) {
  if (value === 'Green') return 'traffic green'
  if (value === 'Red') return 'traffic red'
  return 'traffic gray'
}

async function loadCase() {
  loading.value = true
  error.value = ''
  try {
    const [caseResult, runResult, oneDResult, policyResult] = await Promise.all([
      api<CaseDetail>(`/analysis-cases/${caseId}`),
      api<RunSummary[]>(`/analysis-cases/${caseId}/runs`),
      api<OneDStatus>('/one-d/status').catch(() => ({ configured: false, message: '1D 服務無法連線，仍可自行上傳 Excel' })),
      api<AnalysisPolicy>('/analysis-cases/policy'),
    ])
    detail.value = caseResult
    runs.value = runResult
    oneDStatus.value = oneDResult
    policy.value = policyResult

    const preferred = selectedVersionId.value
      ?? caseResult.case.currentVersionId
      ?? caseResult.versions[0]?.analysisBidVersionId
      ?? null

    if (preferred) {
      await selectVersion(preferred)
    }
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入待偵錯案件'
  } finally {
    loading.value = false
  }
}

async function selectVersion(versionId: number) {
  selectedVersionId.value = versionId
  versionLoading.value = true
  versionDetail.value = null
  quantityScope.value = null
  quantityScopeError.value = ''
  quantityResult.value = null
  priceResult.value = null
  error.value = ''
  try {
    versionDetail.value = await api<VersionDetail>(
      `/analysis-cases/${caseId}/versions/${versionId}`,
    )
    selectedMajorCategory.value = majorCategories.value[0] ?? ''
    // Auxiliary read-only report. Failure must not block older API deployments
    // from displaying the version or its already saved analysis runs.
    try {
      quantityScope.value = await api<QuantityScope>(
        `/analysis-cases/${caseId}/versions/${versionId}/quantity-scope`,
      )
    } catch (caught) {
      quantityScopeError.value = caught instanceof Error
        ? caught.message
        : '無法讀取工項納入範圍'
    }
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入版本'
  } finally {
    versionLoading.value = false
  }
}

function openAddVersion() {
  versionSource.value = 'Upload'
  oneDProjectId.value = null
  versionFile.value = null
  includedSheets.value = []
  workbookReady.value = false
  versionNote.value = ''
  if (versionFileInput.value) versionFileInput.value.value = ''
  showAddVersion.value = true
}

function selectVersionFile(event: Event) {
  includedSheets.value = []
  workbookReady.value = false
  versionFile.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

async function addVersion() {
  versionSaving.value = true
  error.value = ''
  message.value = ''
  try {
    if (versionSource.value === 'OneD') {
      if (!oneDStatus.value.configured) {
        throw new Error('目前環境未設定 1D API，請改用上傳新版標單')
      }
      if (!oneDProjectId.value || oneDProjectId.value <= 0) {
        throw new Error('請輸入有效的 1D ProjectId')
      }

      const created = await api<{ analysisBidVersionId: number }>(
        `/analysis-cases/${caseId}/versions/from-1d/${oneDProjectId.value}`,
        {
          method: 'POST',
          body: JSON.stringify({ note: versionNote.value.trim() }),
        },
      )
      selectedVersionId.value = created.analysisBidVersionId
    } else {
      if (!versionFile.value) throw new Error('請選擇新版待偵錯標單')
      if (!workbookReady.value || !includedSheets.value.length) throw new Error('請預覽並選擇要匯入的工程明細工作表')
      const form = new FormData()
      form.append('excelFile', versionFile.value)
      form.append('includedSheetNames', JSON.stringify(includedSheets.value))
      form.append('note', versionNote.value.trim())
      const created = await apiForm<{ analysisBidVersionId: number }>(
        `/analysis-cases/${caseId}/versions/upload`,
        form,
      )
      selectedVersionId.value = created.analysisBidVersionId
    }

    showAddVersion.value = false
    message.value = '新版本已建立。舊版本仍保留，可切換查看與重新偵錯。'
    await loadCase()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '建立新版本失敗'
  } finally {
    versionSaving.value = false
  }
}

async function runQuantity() {
  if (!selectedVersionId.value) return
  analyzing.value = true
  error.value = ''
  priceResult.value = null
  try {
    quantityResult.value = await api<QuantityRunResponse>(
      `/analysis-cases/${caseId}/versions/${selectedVersionId.value}/quantity-analysis`,
      {
        method: 'POST',
        body: JSON.stringify({ topCount: topCount.value || null }),
      },
    )
    tab.value = 'analysis'
    message.value = `數量異常分析完成，已保存 Run #${quantityResult.value.analysisRunId}。`
    runs.value = await api<RunSummary[]>(`/analysis-cases/${caseId}/runs`)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '數量異常分析失敗'
  } finally {
    analyzing.value = false
  }
}

// 舊版 AnalysisRun.result_json 可能使用 C# PascalCase；即時 API 則為 camelCase。
// 僅轉換歷史結果的物件欄位名稱，不更動已保存的 Run 或分析數值。
function normalizeRunResult(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(normalizeRunResult)
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([key, item]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        normalizeRunResult(item),
      ]),
    )
  }
  return value
}

async function viewRun(run: RunSummary) {
  error.value = ''
  try {
    const saved = await api<AnalysisRunDetail>(
      `/analysis-cases/${caseId}/runs/${run.analysisRunId}`,
    )

    selectedVersionId.value = saved.analysisBidVersionId
    await selectVersion(saved.analysisBidVersionId)

    if (saved.runType === 'Quantity') {
      quantityResult.value = {
        analysisRunId: saved.analysisRunId,
        analysisCaseId: caseId,
        analysisBidVersionId: saved.analysisBidVersionId,
        versionNo: saved.versionNo,
        sourceType: versionDetail.value?.version.sourceType ?? '',
        result: normalizeRunResult(saved.result) as QuantityAnalysis,
      }
      priceResult.value = null
    } else if (saved.runType.startsWith('MajorPrice:')) {
      priceResult.value = {
        analysisRunId: saved.analysisRunId,
        result: normalizeRunResult(saved.result) as PriceAnalysis,
      }
      quantityResult.value = null
    }

    tab.value = 'analysis'
    message.value = `已開啟歷史偵錯 Run #${saved.analysisRunId}（V${saved.versionNo}）。`
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法讀取歷史偵錯結果'
  }
}


async function runPrice() {
  if (!selectedVersionId.value) return
  if (!selectedMajorCategory.value) {
    error.value = '此版本沒有可選擇的工程大項'
    return
  }

  analyzing.value = true
  error.value = ''
  quantityResult.value = null
  try {
    priceResult.value = await api<PriceRunResponse>(
      `/analysis-cases/${caseId}/versions/${selectedVersionId.value}/major-price-analysis`,
      {
        method: 'POST',
        body: JSON.stringify({
          majorCategory: selectedMajorCategory.value,
          topCount: topCount.value || null,
        }),
      },
    )
    tab.value = 'analysis'
    message.value = `大項價格分析完成，已保存 Run #${priceResult.value.analysisRunId}。`
    runs.value = await api<RunSummary[]>(`/analysis-cases/${caseId}/runs`)
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '大項價格分析失敗'
  } finally {
    analyzing.value = false
  }
}

onMounted(loadCase)
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div>
        <RouterLink to="/analysis-cases" class="back-link">← 返回待偵錯案件</RouterLink>
        <h2 class="mt-2">{{ detail?.case.caseName ?? '待偵錯案件' }}</h2>
        <p v-if="detail">
          {{ detail.case.projectType }} ・ {{ detail.case.structureType }} ・ {{ detail.case.city }} ・
          樓地板 {{ formatNumber(detail.case.totalFloorArea) }} m² ・
          基地 {{ formatNumber(detail.case.siteArea) }} m² ・
          {{ detail.case.floorCount }}F / B{{ detail.case.basementFloorCount }}
        </p>
      </div>
      <button class="btn btn-primary" type="button" :disabled="!detail" @click="openAddVersion">
        新增版本
      </button>
    </div>

    <div v-if="error" class="notice error">{{ error }}</div>
    <div v-if="message" class="notice success">{{ message }}</div>

    <div v-if="loading" class="card p-4 text-muted">載入案件中...</div>

    <template v-else-if="detail">
      <section class="version-strip">
        <button
          v-for="version in detail.versions"
          :key="version.analysisBidVersionId"
          type="button"
          class="version-chip"
          :class="{ active: selectedVersionId === version.analysisBidVersionId }"
          @click="selectVersion(version.analysisBidVersionId)"
        >
          <span class="version-title">
            V{{ version.versionNo }}
            <span v-if="version.isCurrent" class="current-mark">目前</span>
          </span>
          <span>{{ sourceLabel(version.sourceType) }}</span>
          <small>{{ version.itemCount }} 筆工項</small>
        </button>
      </section>

      <section v-if="versionDetail" class="card">
        <div class="card-header version-header">
          <div>
            <h3>Version {{ versionDetail.version.versionNo }}</h3>
            <div class="text-muted small mt-1">
              {{ sourceLabel(versionDetail.version.sourceType) }}
              <template v-if="versionDetail.version.sourceOneDProjectId">
                ・1D ProjectId {{ versionDetail.version.sourceOneDProjectId }}
              </template>
              <template v-if="versionDetail.version.sourceFileName">
                ・{{ versionDetail.version.sourceFileName }}
              </template>
              ・{{ formatDate(versionDetail.version.createdAt) }}
            </div>
          </div>
          <span v-if="versionDetail.version.sourceNote" class="version-note">
            {{ versionDetail.version.sourceNote }}
          </span>
        </div>

        <div class="card-body analysis-actions">
          <div>
            <strong>對這個版本執行偵錯</strong>
            <div class="text-muted small mt-1">
              分析只讀取此版本快照，不會重新呼叫 1D，也不會寫入歷史基準。
              目前暫定：鋼筋、混凝土做細項數量偵錯；結構體工程做大項估算與價格異常。
            </div>
          </div>

          <div class="action-controls">
            <label>
              <span>Top K（選填）</span>
              <input v-model.number="topCount" type="number" min="1" class="form-control" placeholder="系統預設" />
            </label>
            <button class="btn btn-primary" type="button" :disabled="analyzing" @click="runQuantity">
              {{ analyzing ? '分析中...' : '數量偵錯' }}
            </button>
            <select v-model="selectedMajorCategory" class="form-select" :disabled="!majorCategories.length">
              <option value="">選擇估算大項</option>
              <option v-for="category in majorCategories" :key="category" :value="category">{{ category }}</option>
            </select>
            <button class="btn btn-outline-primary" type="button" :disabled="analyzing || !selectedMajorCategory" @click="runPrice">
              價格異常
            </button>
          </div>
        </div>
      </section>

      <section v-if="versionDetail" class="card quantity-scope-panel">
        <div class="card-header scope-heading">
          <div>
            <h3>數量偵錯工項納入範圍</h3>
            <p class="text-muted small mt-1 mb-0">每筆工項是否計入鋼筋／混凝土數量分析，由後端正式前處理規則判斷；與大項價格總額的納入規則不同。</p>
          </div>
        </div>
        <div v-if="quantityScope" class="scope-body">
          <div class="scope-counts">
            <div>版本總工項 <strong>{{ quantityScope.itemCount }}</strong></div>
            <div>數量偵錯納入 <strong>{{ quantityScope.includedItemCount }}</strong></div>
            <div>數量偵錯排除 <strong>{{ quantityScope.excludedItemCount }}</strong></div>
            <div>待確認 <strong>{{ quantityScope.pendingItemCount }}</strong></div>
          </div>
          <div v-if="quantityScope.excludedQuantityCandidateCount" class="notice info mb-2">
            <strong>{{ quantityScope.excludedQuantityCandidateCount }} 筆材料相關名稱工項被排除。</strong>
            含加工、組立、綁紮、澆置等複合工項可能同時包含材料與勞務；此處僅呈現系統判定，請核對原始標單，不得直接假設材料數量為零。
          </div>
          <p class="text-muted small">{{ quantityScope.note }}</p>
          <details class="scope-details" :open="quantityScope.excludedQuantityCandidateCount > 0">
            <summary>查看排除／待確認工項及原因（{{ scopeReviewRows.length }} 筆）</summary>
            <div v-if="!scopeReviewRows.length" class="text-muted small mt-2">沒有排除或待確認的工項。</div>
            <div v-else class="table-wrap mt-2">
              <table class="scope-table">
                <thead>
                  <tr>
                    <th>來源</th>
                    <th>工程大項</th>
                    <th>工項</th>
                    <th>數量</th>
                    <th>分類</th>
                    <th>原因</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in scopeReviewRows" :key="item.analysisBidItemId">
                    <td>{{ item.sheetName ?? '-' }}{{ item.rowNumber ? ' #' + item.rowNumber : '' }}</td>
                    <td>{{ item.majorCategory || '未分類' }}</td>
                    <td class="item-name">{{ item.itemName }}<span v-if="item.spec" class="text-muted small"> ・ {{ item.spec }}</span></td>
                    <td class="number">{{ formatNumber(item.quantity, 4) }} {{ item.unit ?? '' }}</td>
                    <td>{{ item.status === 'Pending' ? '待確認' : '排除' }}<span v-if="item.candidateType === 'Excluded'" class="text-muted small"> ・疑似複合工項</span></td>
                    <td>{{ item.reason }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </details>
        </div>
        <div v-else-if="quantityScopeError" class="scope-body">
          <div class="notice info mb-0">無法取得目前版本的工項納入範圍：{{ quantityScopeError }}。既有分析結果仍可查看；請確認後端是否已更新。</div>
        </div>
        <div v-else class="scope-body text-muted small">讀取工項分類中...</div>
      </section>

      <MaterialCapabilityPanel :structure-type="detail.case.structureType" />
      <BulkMaterialEstimatePanel :target="detail.case" />

      <div class="tabs">
        <button class="tab" :class="{ active: tab === 'items' }" @click="tab = 'items'">版本工項</button>
        <button class="tab" :class="{ active: tab === 'analysis' }" @click="tab = 'analysis'">本次分析結果</button>
        <button class="tab" :class="{ active: tab === 'runs' }" @click="tab = 'runs'">歷次偵錯</button>
      </div>

      <section v-if="tab === 'items'" class="card">
        <div v-if="versionLoading" class="p-4 text-muted">載入版本工項中...</div>
        <div v-else-if="!versionDetail?.items.length" class="empty-state">此版本沒有工項資料。</div>
        <div v-else class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>工程大項</th>
                <th>工項</th>
                <th>規格</th>
                <th>單位</th>
                <th>數量</th>
                <th>單價</th>
                <th>金額</th>
                <th>來源</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in versionDetail.items" :key="item.analysisBidItemId">
                <td>
                  <span v-if="item.majorCategory" class="major-tag">{{ item.majorCategory }}</span>
                  <span v-else class="text-danger">未分類</span>
                </td>
                <td class="item-name">{{ item.itemName || '-' }}</td>
                <td>{{ item.spec || '-' }}</td>
                <td>{{ item.unit || '-' }}</td>
                <td class="number">{{ formatNumber(item.quantity, 4) }}</td>
                <td class="number">{{ formatNumber(item.unitPrice) }}</td>
                <td class="number">{{ formatNumber(item.amount) }}</td>
                <td>
                  <span v-if="versionDetail.version.sourceType === 'OneD'">
                    1D<span v-if="item.sourceConfidence != null"> ・ {{ Math.round(item.sourceConfidence * 100) }}%</span>
                  </span>
                  <span v-else>{{ item.sheetName || '上傳檔案' }}<span v-if="item.rowNumber"> #{{ item.rowNumber }}</span></span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section v-else-if="tab === 'analysis'" class="card">
        <div v-if="!quantityResult && !priceResult" class="empty-state">
          尚未對目前選取的版本執行分析。
        </div>

        <template v-if="quantityResult">
          <div class="card-header">
            <div>
              <h3>數量偵錯</h3>
              <div class="text-muted small mt-1">
                可分析 {{ quantityResult.result.analyzedGroupCount }} 組 ・
                樣本不足 {{ quantityResult.result.insufficientDataCount }} 組 ・
                略過 {{ quantityResult.result.skippedGroupCount }} 組
              </div>
            </div>
          </div>
          <div class="notice info" style="margin: 12px 20px;">歷史案例先按樓地板面積換算單位用量，再以本案面積轉成 P15／P50／P85 預估總量；異常判定比較的是同一單位的材料總數量。區間是歷史統計參考，並非工程設計上限。</div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>工程大項</th>
                  <th>偵錯細項</th>
                  <th>實際數量</th>
                  <th>本案單位用量（材料單位/m²）</th>
                  <th>P15 預估總量</th>
                  <th>P50 預估總量</th>
                  <th>P85 預估總量</th>
                  <th>參考案</th>
                  <th>判定</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in quantityResult.result.items" :key="`${item.majorCategoryName}-${item.detectionCategoryName}`">
                  <td>{{ item.majorCategoryName }}</td>
                  <td><strong>{{ item.detectionCategoryName }}</strong></td>
                  <td class="number">{{ formatNumber(item.observedQuantity, 4) }} {{ item.unitName ?? '' }}</td>
                  <td class="number">{{ formatNumber(item.observedUsagePerArea, 6) }}</td>
                  <td class="number">{{ formatNumber(item.range?.lowerQuantity, 2) }} {{ item.unitName ?? '' }}</td>
                  <td class="number">{{ formatNumber(item.range?.medianQuantity, 2) }} {{ item.unitName ?? '' }}</td>
                  <td class="number">{{ formatNumber(item.range?.upperQuantity, 2) }} {{ item.unitName ?? '' }}</td>
                  <td>{{ item.range?.referenceProjectCount ?? 0 }}</td>
                  <td>
                    <span :class="trafficClass(item.trafficLight)"></span>
                    {{ statusLabel(item.analysisStatus) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-for="item in quantityResult.result.items" :key="'reference-' + item.detectionCategoryName" style="margin: 12px 20px;">
            <details v-if="item.range?.references?.length">
              <summary style="cursor: pointer; font-weight: 600;">
                查看 {{ item.detectionCategoryName }} 歷史參考案件明細（{{ item.range.references.length }} 案）
              </summary>
              <p class="text-muted small mt-2">實際採用的歷史案例與權重；每案先換算為單位面積用量，再依相似度及時間權重計算分位數。此表只供來源核對，不會更改既有分析結果。</p>
              <p class="text-muted small">權重有效樣本數 ESS：<strong>{{ formatNumber(item.range.effectiveSampleSize, 2) }}</strong>；最大單一案例權重占比：<strong>{{ formatNumber(item.range.largestWeightSharePercent, 2) }}%</strong>。這兩項描述樣本權重集中情況，不代表異常判定正確率。</p>
              <p v-if="item.range.statusMessage" class="text-muted small">{{ item.range.statusMessage }}</p>
              <div v-if="item.range.references.some(reference => isRecognizableDemoName(reference.projectName))" class="notice info" style="margin: 10px 0;">
                <strong>參考資料包含名稱可辨識的模擬／測試建案。</strong>
                此分析可用於流程驗證，但尚不能視為真實工程數量模型的有效性驗證。建案是否涵蓋完整工程範圍也尚未有獨立欄位可查；此提醒不會變更既有 Run 結果。
              </div>
              <div class="table-wrap">
                <table>
                  <thead><tr><th>建案</th><th>日期</th><th>樓地板面積 m²</th><th>材料數量</th><th>單位用量</th><th>相似度 %</th><th>相似權重</th><th>時間權重</th><th>最終權重</th></tr></thead>
                  <tbody>
                    <tr v-for="reference in item.range.references" :key="reference.projectId">
                      <td>{{ reference.projectName }} (#{{ reference.projectId }}) <span v-if="isRecognizableDemoName(reference.projectName)" class="text-danger">〔疑似模擬／測試〕</span></td>
                      <td>{{ reference.projectDate?.slice(0, 10) }}</td>
                      <td class="number">{{ formatNumber(reference.floorArea, 2) }}</td>
                      <td class="number">{{ formatNumber(reference.actualQuantity, 2) }} {{ item.unitName ?? '' }}</td>
                      <td class="number">{{ formatNumber(reference.usagePerArea, 6) }} {{ item.unitName ?? '' }}/m²</td>
                      <td class="number">{{ formatNumber(reference.similarityScore, 2) }}</td>
                      <td class="number">{{ formatNumber(reference.similarityWeight, 6) }}</td>
                      <td class="number">{{ formatNumber(reference.timeWeight, 6) }}</td>
                      <td class="number">{{ formatNumber(reference.finalWeight, 6) }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </details>
          </div>
        </template>

        <template v-if="priceResult">
          <div class="card-header">
            <div>
              <h3>價格異常：{{ priceResult.result.majorCategory }}</h3>
              <div class="text-muted small mt-1">{{ priceResult.result.statusMessage }}</div>
            </div>
            <div class="result-status">
              <span :class="trafficClass(priceResult.result.trafficLight)"></span>
              {{ statusLabel(priceResult.result.analysisStatus) }}
            </div>
          </div>
          <div class="price-grid">
            <div><span>實際總價</span><strong>{{ formatNumber(priceResult.result.observedAmount) }}</strong></div>
            <div><span>實際每坪單價</span><strong>{{ formatNumber(priceResult.result.observedPricePerPing) }}</strong></div>
            <div><span>P15 每坪單價</span><strong>{{ formatNumber(priceResult.result.lowerPricePerPing) }}</strong></div>
            <div><span>P50 每坪單價</span><strong>{{ formatNumber(priceResult.result.medianPricePerPing) }}</strong></div>
            <div><span>P85 每坪單價</span><strong>{{ formatNumber(priceResult.result.upperPricePerPing) }}</strong></div>
            <div><span>有效參考案</span><strong>{{ priceResult.result.referenceProjectCount }}</strong></div>
            <div><span>權重有效樣本數 ESS</span><strong>{{ formatNumber(priceResult.result.effectiveSampleSize, 2) }}</strong></div>
            <div><span>最大案例權重占比</span><strong>{{ formatNumber(priceResult.result.largestWeightSharePercent, 2) }}%</strong></div>
          </div>
        </template>
      </section>

      <section v-else class="card">
        <div v-if="!runs.length" class="empty-state">這個案件尚未執行過偵錯。</div>
        <div v-else class="table-wrap">
          <table>
            <thead><tr><th>Run</th><th>版本</th><th>分析類型</th><th>狀態</th><th>執行時間</th><th>操作</th></tr></thead>
            <tbody>
              <tr v-for="run in runs" :key="run.analysisRunId">
                <td>#{{ run.analysisRunId }}</td>
                <td>V{{ run.versionNo }}</td>
                <td>{{ run.runType === 'Quantity' ? '數量偵錯' : run.runType.replace('MajorPrice:', '大項估算／價格：') }}</td>
                <td>{{ run.status }}</td>
                <td>{{ formatDate(run.createdAt) }}</td>
                <td>
                  <button class="btn btn-sm btn-outline-primary" type="button" @click="viewRun(run)">
                    查看結果
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>

    <div v-if="showAddVersion" class="modal-backdrop-custom" @mousedown.self="showAddVersion = false">
      <div class="modal-dialog modal-lg" role="dialog" aria-modal="true">
        <form class="modal-content" @submit.prevent="addVersion">
          <div class="modal-header">
            <div>
              <h5 class="modal-title">新增待偵錯版本</h5>
              <div class="text-muted small mt-1">建立新快照，不覆蓋舊版本。</div>
            </div>
            <button type="button" class="btn-close" @click="showAddVersion = false"></button>
          </div>
          <div class="modal-body">
            <div class="source-choice mb-4">
              <button type="button" class="source-option" :class="{ active: versionSource === 'Upload' }" @click="versionSource = 'Upload'">
                <strong>上傳新版標單</strong>
                <span>2A 獨立流程；適合同一案件修改數量或價格後再次送估。</span>
              </button>
              <button
                type="button"
                class="source-option"
                :class="{ active: versionSource === 'OneD', unavailable: !oneDStatus.configured }"
                :disabled="!oneDStatus.configured"
                @click="oneDStatus.configured && (versionSource = 'OneD')"
              >
                <strong>重新從 1D 匯入</strong>
                <span v-if="oneDStatus.configured">產學整合環境才使用；只有需要取得 1D 最新結果時才重新匯入。</span>
                <span v-else>目前環境未連接 1D，請直接上傳新版標單。</span>
              </button>
            </div>

            <label v-if="versionSource === 'Upload'" class="field">
              <span>新版待偵錯標單</span>
              <input ref="versionFileInput" type="file" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" required @change="selectVersionFile" />
            </label>
            <WorkbookSheetPicker v-if="versionSource === 'Upload'" :file="versionFile" v-model="includedSheets" @update:ready="workbookReady = $event" />
            <label v-else class="field">
              <span>1D ProjectId</span>
              <input v-model.number="oneDProjectId" type="number" min="1" required placeholder="輸入 1D ProjectId" />
            </label>

            <label class="field mt-3">
              <span>版本備註（選填）</span>
              <textarea v-model="versionNote" rows="3" placeholder="例如：廠商第二版、修正鋼筋數量後"></textarea>
            </label>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showAddVersion = false">取消</button>
            <button type="submit" class="btn btn-primary" :disabled="versionSaving">
              {{ versionSaving ? '建立中...' : '建立新版本' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.quantity-scope-panel .scope-heading { align-items: flex-start; }
.scope-body { padding: 16px 20px 20px; }
.scope-counts { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 14px; }
.scope-counts > div { padding: 12px; background: #f7f9fc; border: 1px solid var(--border); border-radius: 7px; font-size: 13px; }
.scope-counts strong { display: block; margin-top: 4px; font-size: 19px; }
.scope-details summary { cursor: pointer; font-weight: 700; }
.table-wrap .scope-table { min-width: 800px; }
.scope-table th, .scope-table td { font-size: 13px; }
@media (max-width: 640px) { .scope-counts { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
.back-link { color: var(--wz-primary); text-decoration: none; font-size: 13px; font-weight: 700; }
.version-strip { display: flex; gap: 10px; overflow-x: auto; padding-bottom: 2px; }
.version-chip { min-width: 145px; display: grid; gap: 3px; padding: 12px 14px; text-align: left; color: #526079; background: #fff; border: 1px solid var(--border); border-radius: 8px; }
.version-chip.active { color: var(--wz-primary); border-color: var(--wz-primary); background: var(--blue-soft); }
.version-chip small { color: var(--muted); }
.version-title { display: flex; align-items: center; gap: 7px; font-weight: 800; }
.current-mark { padding: 2px 5px; color: #176e50; background: #e8f8f1; border-radius: 5px; font-size: 10px; }
.version-header { align-items: flex-start; }
.version-note { max-width: 42%; padding: 7px 10px; color: #526079; background: #f7f9fc; border-radius: 6px; font-size: 12px; }
.analysis-actions { display: flex; flex-direction: column; align-items: stretch; gap: 16px; }
.analysis-actions > div { min-width: 0; }
.action-controls { display: grid; width: 100%; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 12px; align-items: end; }
.action-controls > * { min-width: 0; width: 100%; }
.action-controls label span { display: block; margin-bottom: 5px; color: var(--muted); font-size: 11px; font-weight: 700; }
.action-controls .btn, .action-controls .form-control, .action-controls .form-select { min-height: 44px; width: 100%; }
.action-controls .btn { display: flex; align-items: center; justify-content: center; padding: 9px 14px; line-height: 1.35; white-space: normal; text-align: center; }
.table-wrap { max-width: 100%; overflow-x: auto; }
.table-wrap table { min-width: 1100px; width: 100%; table-layout: auto; }
.table-wrap th, .table-wrap td { white-space: normal; overflow-wrap: anywhere; vertical-align: top; }
.table-wrap td.item-name { min-width: 290px; max-width: 480px; line-height: 1.5; }
.table-wrap td.number { white-space: nowrap; }
.item-name { font-weight: 700; }
.number { text-align: right; font-variant-numeric: tabular-nums; }
.major-tag { display: inline-block; padding: 4px 7px; color: #34425d; background: #eef1f5; border-radius: 5px; font-size: 12px; font-weight: 700; }
.traffic { display: inline-block; width: 9px; height: 9px; margin-right: 5px; border-radius: 50%; }
.traffic.green { background: var(--wz-success); }
.traffic.red { background: var(--wz-danger); }
.traffic.gray { background: #8a949e; }
.result-status { font-weight: 800; }
.price-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 0; }
.price-grid div { padding: 18px; border-right: 1px solid #edf0f4; }
.price-grid div:last-child { border-right: 0; }
.price-grid span { display: block; color: var(--muted); font-size: 12px; }
.price-grid strong { display: block; margin-top: 7px; font-size: 18px; }
.source-choice { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.source-option { display: grid; gap: 5px; padding: 16px; text-align: left; color: #34425d; background: #fff; border: 1px solid var(--border); border-radius: 8px; }
.source-option span { color: var(--muted); font-size: 13px; line-height: 1.5; }
.source-option.active { color: var(--wz-primary); border-color: var(--wz-primary); background: var(--blue-soft); }
.source-option.unavailable, .source-option:disabled { opacity: .55; cursor: not-allowed; background: #f7f8fa; }
.modal-backdrop-custom { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; background: rgba(0,0,0,.5); }
.modal-backdrop-custom .modal-dialog { width: min(760px, 100%); margin: auto; }
@media (max-width: 1100px) {
  .analysis-actions { align-items: stretch; flex-direction: column; }
  .action-controls { width: 100%; grid-template-columns: 1fr 1fr; }
  .price-grid { grid-template-columns: repeat(3, 1fr); }
}
@media (max-width: 640px) {
  .action-controls, .source-choice, .price-grid { grid-template-columns: 1fr; }
  .version-note { max-width: 100%; }
}
</style>
