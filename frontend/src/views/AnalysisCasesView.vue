<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ProjectFields from '../components/ProjectFields.vue'
import WorkbookSheetPicker from '../components/WorkbookSheetPicker.vue'
import { api, apiForm } from '../services/api'
import { useSystemOptions } from '../composables/useSystemOptions'
import type { ProjectForm } from '../types'

interface AnalysisCaseSummary {
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

interface OneDStatus {
  configured: boolean
  message: string
}

interface ReadinessItem {
  detectionCategoryId: number
  majorCategoryName: string
  detectionCategoryName: string
  unitName: string
  mappedMaterialCount: number
  historicalTransactionCount: number
  historicalProjectCount: number
  minimumSampleCount: number
  missingProjectCount: number
  status: string
  message: string
}

interface ReadinessResponse {
  categoryCount: number
  minimumSampleCount: number
  readyCount: number
  items: ReadinessItem[]
}

interface PolicyResponse {
  provisional: boolean
  oneDUsage: string
  selfUpload: {
    enabled: boolean
    mode: string
    requirement: string
    priceFields: string
  }
}

const { options, error: optionsError } = useSystemOptions()
const cases = ref<AnalysisCaseSummary[]>([])
const policy = ref<PolicyResponse | null>(null)
const readiness = ref<ReadinessResponse | null>(null)
const oneDStatus = ref<OneDStatus>({ configured: false, message: '1D API 尚未設定' })
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const message = ref('')
const showCreate = ref(false)
const sourceType = ref<'OneD' | 'Upload'>('Upload')
const oneDProjectId = ref<number | null>(null)
const uploadFile = ref<File | null>(null)
const includedSheets = ref<string[]>([])
const workbookReady = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const note = ref('')
const form = ref<ProjectForm>(emptyForm())

function emptyForm(): ProjectForm {
  return {
    projectName: '',
    projectType: '',
    structureType: '',
    city: '',
    totalFloorArea: null,
    siteArea: null,
    floorCount: null,
    basementFloorCount: 0,
    projectMonth: '',
  }
}

function resetCreate() {
  sourceType.value = oneDStatus.value.configured ? 'OneD' : 'Upload'
  oneDProjectId.value = null
  uploadFile.value = null
  includedSheets.value = []
  workbookReady.value = false
  note.value = ''
  form.value = emptyForm()
  if (fileInput.value) fileInput.value.value = ''
  error.value = ''
}

function openCreate() {
  resetCreate()
  showCreate.value = true
}

function selectFile(event: Event) {
  includedSheets.value = []
  workbookReady.value = false
  uploadFile.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

function formatNumber(value: number) {
  return Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}

function formatDate(value?: string | null) {
  return value ? value.slice(0, 10) : '未設定'
}

function sourceLabel(value?: string | null) {
  return value === 'OneD' ? '1D 匯入' : value === 'Upload' ? '自行上傳' : '尚無版本'
}

function validateCommon() {
  const value = form.value
  if (!value.projectName.trim()) throw new Error('請輸入案件名稱')
  if (!value.projectType) throw new Error('請選擇建案類型')
  if (!value.structureType) throw new Error('請選擇結構類型')
  if (!value.city) throw new Error('請選擇縣市')
  if (!value.totalFloorArea || value.totalFloorArea <= 0) throw new Error('請輸入有效的總樓地板面積')
  if (!value.siteArea || value.siteArea <= 0) throw new Error('請輸入有效的基地面積')
  if (!value.floorCount || value.floorCount <= 0) throw new Error('請輸入有效的地上樓層')
  if (value.basementFloorCount == null || value.basementFloorCount < 0) throw new Error('請輸入有效的地下樓層')
  if (!value.projectMonth) throw new Error('請選擇建案年月')
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [caseResult, policyResult, oneDResult, readinessResult] = await Promise.all([
      api<AnalysisCaseSummary[]>('/analysis-cases'),
      api<PolicyResponse>('/analysis-cases/policy'),
      api<OneDStatus>('/one-d/status'),
      api<ReadinessResponse>('/detection-category/readiness'),
    ])
    cases.value = caseResult
    policy.value = policyResult
    oneDStatus.value = oneDResult
    readiness.value = readinessResult
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入待偵錯案件'
  } finally {
    loading.value = false
  }
}

async function createCase() {
  saving.value = true
  error.value = ''
  message.value = ''

  try {
    validateCommon()
    const value = form.value
    const projectDate = `${value.projectMonth}-01`

    if (sourceType.value === 'OneD') {
      if (!oneDStatus.value.configured) {
        throw new Error('目前環境未設定 1D API，請改用自行上傳建立待偵錯案件')
      }
      if (!oneDProjectId.value || oneDProjectId.value <= 0) {
        throw new Error('請輸入有效的 1D ProjectId')
      }

      await api(`/analysis-cases/from-1d/${oneDProjectId.value}?note=${encodeURIComponent(note.value.trim())}`, {
        method: 'POST',
        body: JSON.stringify({
          caseName: value.projectName.trim(),
          projectType: value.projectType,
          structureType: value.structureType,
          city: value.city,
          totalFloorArea: value.totalFloorArea,
          siteArea: value.siteArea,
          floorCount: value.floorCount,
          basementFloorCount: value.basementFloorCount,
          projectDate,
        }),
      })
    } else {
      if (!uploadFile.value) {
        throw new Error('請選擇待偵錯標單')
      }

      if (!workbookReady.value || !includedSheets.value.length) {
        throw new Error('請先預覽並選擇本案要匯入的工程明細工作表')
      }
      const data = new FormData()
      data.append('excelFile', uploadFile.value)
      data.append('includedSheetNames', JSON.stringify(includedSheets.value))
      data.append('caseName', value.projectName.trim())
      data.append('projectType', value.projectType)
      data.append('structureType', value.structureType)
      data.append('city', value.city)
      data.append('totalFloorArea', String(value.totalFloorArea))
      data.append('siteArea', String(value.siteArea))
      data.append('floorCount', String(value.floorCount))
      data.append('basementFloorCount', String(value.basementFloorCount))
      data.append('projectDate', projectDate)
      data.append('note', note.value.trim())

      await apiForm('/analysis-cases/from-upload', data)
    }

    showCreate.value = false
    message.value = '待偵錯案件已建立，來源資料已保存為 Version 1。'
    await load()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '建立待偵錯案件失敗'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-stack">
    <div class="page-intro">
      <div>
        <h2>待偵錯案件</h2>
        <p>待偵錯資料與已決標歷史資料分開管理。1D 或自行上傳只負責建立 2A 的版本快照，後續可反覆偵錯。</p>
      </div>
      <button class="btn btn-primary" type="button" @click="openCreate">新增待偵錯案件</button>
    </div>

    <div v-if="optionsError || error" class="notice error">{{ optionsError || error }}</div>
    <div v-if="message" class="notice success">{{ message }}</div>

    <section v-if="policy?.provisional" class="notice info">
      <strong>目前依暫定流程實作：</strong>
      2A 可獨立用「自行上傳」運作；有連接 1D 的產學環境則會額外開放「從 1D 匯入」。
      偵錯範圍先以「鋼筋、混凝土」作為細項數量偵錯單位，並以「結構體工程」作為大項估算／價格異常單位。
    </section>

    <section v-if="readiness" class="history-readiness card">
      <div class="card-header readiness-head">
        <div>
          <h3>歷史基準狀態</h3>
          <div class="text-muted small mt-1">
            細項數量偵錯至少需要 {{ readiness.minimumSampleCount }} 個有效歷史建案。
            目前只追蹤鋼筋與混凝土。
          </div>
        </div>
        <RouterLink to="/projects" class="btn btn-sm btn-outline-primary">前往建案管理</RouterLink>
      </div>
      <div class="readiness-grid">
        <div v-for="item in readiness.items" :key="item.detectionCategoryId" class="readiness-item">
          <div class="readiness-title">
            <strong>{{ item.detectionCategoryName }}</strong>
            <span>{{ item.unitName }}</span>
          </div>
          <div class="readiness-count">
            <strong>{{ item.historicalProjectCount }}</strong>
            <span>/ {{ item.minimumSampleCount }} 案</span>
          </div>
          <div class="readiness-bar">
            <span
              :style="{ width: Math.min(100, (item.historicalProjectCount / item.minimumSampleCount) * 100) + '%' }"
            ></span>
          </div>
          <div class="small" :class="item.status === 'ReadyForEstimation' ? 'text-success' : 'text-muted'">
            {{ item.message }}
          </div>
        </div>
      </div>
    </section>

    <section class="stat-grid">
      <div class="stat"><span>待偵錯案件</span><strong>{{ cases.length }}</strong></div>
      <div class="stat"><span>目前版本總數</span><strong>{{ cases.reduce((sum, item) => sum + item.versionCount, 0) }}</strong></div>
      <div class="stat"><span>目前來源：1D</span><strong>{{ cases.filter(x => x.currentSourceType === 'OneD').length }}</strong></div>
      <div class="stat"><span>目前來源：自行上傳</span><strong>{{ cases.filter(x => x.currentSourceType === 'Upload').length }}</strong></div>
    </section>

    <div v-if="loading" class="card p-4 text-muted">載入待偵錯案件中...</div>
    <div v-else-if="!cases.length" class="card empty-state">
      尚未建立待偵錯案件。{{ oneDStatus.configured ? '可從 1D 匯入，或直接上傳結構化標單。' : '目前未連接 1D，請直接上傳結構化標單建立案件。' }}
    </div>

    <section v-else class="case-grid">
      <RouterLink
        v-for="item in cases"
        :key="item.analysisCaseId"
        :to="`/analysis-cases/${item.analysisCaseId}`"
        class="case-card"
      >
        <div class="case-card-head">
          <div>
            <h3>{{ item.caseName }}</h3>
            <div class="text-muted small">
              {{ item.projectType }} ・ {{ item.structureType }} ・ {{ item.city }}
            </div>
          </div>
          <span class="source-badge" :class="item.currentSourceType === 'OneD' ? 'one-d' : 'upload'">
            {{ sourceLabel(item.currentSourceType) }}
          </span>
        </div>

        <div class="case-meta">
          <div><span>總樓地板面積</span><strong>{{ formatNumber(item.totalFloorArea) }} m²</strong></div>
          <div><span>基地面積</span><strong>{{ formatNumber(item.siteArea) }} m²</strong></div>
          <div><span>樓層</span><strong>{{ item.floorCount }}F / B{{ item.basementFloorCount }}</strong></div>
          <div><span>建案日期</span><strong>{{ formatDate(item.projectDate) }}</strong></div>
          <div><span>版本</span><strong>{{ item.versionCount }} 版</strong></div>
        </div>

        <div class="case-card-foot">
          <span>目前版本：V{{ item.currentVersionNo ?? '-' }}</span>
          <span>查看案件 →</span>
        </div>
      </RouterLink>
    </section>

    <div v-if="showCreate" class="modal-backdrop-custom" @mousedown.self="showCreate = false">
      <div class="modal-dialog modal-xl" role="dialog" aria-modal="true" aria-labelledby="create-analysis-title">
        <form class="modal-content" @submit.prevent="createCase">
          <div class="modal-header">
            <div>
              <h5 id="create-analysis-title" class="modal-title">新增待偵錯案件</h5>
              <div class="text-muted small mt-1">建立的是 Target Case，不會寫入 HistoricalTransaction。</div>
            </div>
            <button type="button" class="btn-close" aria-label="關閉" @click="showCreate = false"></button>
          </div>

          <div class="modal-body">
            <div class="source-choice mb-4">
              <button
                type="button"
                class="source-option"
                :class="{ active: sourceType === 'Upload' }"
                @click="sourceType = 'Upload'"
              >
                <strong>自行上傳</strong>
                <span>2A 可獨立使用。專題環境沒有 1D 時，直接從這裡建立待偵錯案件。</span>
              </button>
              <button
                type="button"
                class="source-option"
                :class="{ active: sourceType === 'OneD', unavailable: !oneDStatus.configured }"
                :disabled="!oneDStatus.configured"
                @click="oneDStatus.configured && (sourceType = 'OneD')"
              >
                <strong>從 1D 匯入</strong>
                <span v-if="oneDStatus.configured">產學整合環境使用；讀取一次 1D 結果後存成 2A 快照。</span>
                <span v-else>目前環境未連接 1D；不影響 2A 自行上傳與偵錯。</span>
              </button>
            </div>

            <ProjectFields
              v-model="form"
              :options="options"
              include-name
              name-label="案件名稱"
              name-placeholder="例如：A 集合住宅第一次估價"
              area-label="總樓地板面積（m²）"
              floor-label="地上樓層"
              basement-floor-label="地下樓層"
            />

            <div v-if="sourceType === 'OneD'" class="source-fields mt-4">
              <label class="field">
                <span>1D ProjectId</span>
                <input v-model.number="oneDProjectId" type="number" min="1" placeholder="輸入已完成 1D 的 ProjectId" required />
              </label>
              <div class="notice info mb-0">
                只在建立版本時讀取 1D。匯入完成後即使 1D 不再執行，2A 仍可使用自己的快照反覆偵錯。
              </div>
            </div>

            <div v-else class="source-fields mt-4">
              <label class="field">
                <span>待偵錯標單</span>
                <input
                  ref="fileInput"
                  type="file"
                  accept=".xlsx,.xls,.xlsm,.xlsb,.csv"
                  required
                  @change="selectFile"
                />
              </label>
              <WorkbookSheetPicker :file="uploadFile" v-model="includedSheets" @update:ready="workbookReady = $event" />
              <div class="notice info mb-0">
                暫定要求：標單解析後須具有工程大項、工項名稱或規格、單位與數量。若沒有工程大項分類，系統目前會拒絕建檔而不是自行猜測。
              </div>
            </div>

            <label class="field mt-4">
              <span>版本備註（選填）</span>
              <textarea v-model="note" rows="2" placeholder="例如：第一次送估、廠商修正版、調整鋼筋數量後"></textarea>
            </label>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showCreate = false">取消</button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? '建立中...' : '建立案件與 V1' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.case-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.case-card { display: block; padding: 20px; color: inherit; background: #fff; border: 1px solid var(--border); border-radius: 8px; box-shadow: var(--shadow); text-decoration: none; transition: transform .15s ease, border-color .15s ease; }
.case-card:hover { transform: translateY(-1px); border-color: #aeb7c5; }
.case-card-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; }
.case-card h3 { margin: 0 0 6px; font-size: 18px; }
.case-meta { display: grid; grid-template-columns: repeat(5, 1fr); gap: 12px; margin-top: 20px; }
.case-meta div { min-width: 0; }
.case-meta span { display: block; color: var(--muted); font-size: 12px; }
.case-meta strong { display: block; margin-top: 4px; font-size: 14px; }
.case-card-foot { display: flex; justify-content: space-between; margin-top: 18px; padding-top: 14px; border-top: 1px solid #edf0f4; color: var(--muted); font-size: 13px; }
.source-badge { padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.source-badge.one-d { color: #214f9c; background: #e8f0ff; }
.source-badge.upload { color: #176e50; background: #e8f8f1; }
.source-choice { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.source-option { display: grid; gap: 5px; padding: 16px; text-align: left; color: #34425d; background: #fff; border: 1px solid var(--border); border-radius: 8px; }
.source-option span { color: var(--muted); font-size: 13px; line-height: 1.5; }
.source-option.active { color: var(--wz-primary); border-color: var(--wz-primary); background: var(--blue-soft); }
.source-option.unavailable, .source-option:disabled { opacity: .55; cursor: not-allowed; background: #f7f8fa; }
.source-fields { display: grid; grid-template-columns: minmax(260px, .6fr) 1fr; gap: 16px; align-items: end; }
.modal-backdrop-custom { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; overflow-y: auto; background: rgba(0,0,0,.5); }
.modal-backdrop-custom .modal-dialog { width: min(1050px, 100%); margin: auto; }
.modal-content { max-height: calc(100vh - 3rem); overflow-y: auto; }
.modal-backdrop-custom :deep(.form-grid) { grid-template-columns: repeat(4, minmax(0, 1fr)); }
.modal-backdrop-custom :deep(.form-grid .span-2:first-child) { grid-column: span 2; }
.history-readiness { overflow: hidden; }
.readiness-head { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.readiness-head h3 { margin: 0; font-size: 16px; }
.readiness-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 0; }
.readiness-item { padding: 18px; border-right: 1px solid #edf0f4; }
.readiness-item:last-child { border-right: 0; }
.readiness-title { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; }
.readiness-title span { color: var(--muted); font-size: 12px; }
.readiness-count { display: flex; align-items: baseline; gap: 6px; margin-top: 12px; }
.readiness-count strong { font-size: 24px; }
.readiness-count span { color: var(--muted); font-size: 13px; }
.readiness-bar { height: 7px; margin: 10px 0; overflow: hidden; background: #edf0f4; border-radius: 999px; }
.readiness-bar span { display: block; height: 100%; background: var(--wz-primary); border-radius: inherit; }

@media (max-width: 1050px) {
  .case-grid { grid-template-columns: 1fr; }
  .readiness-grid { grid-template-columns: 1fr; }
  .readiness-item { border-right: 0; border-bottom: 1px solid #edf0f4; }
  .readiness-item:last-child { border-bottom: 0; }
  .case-meta { grid-template-columns: 1fr 1fr; }
  .modal-backdrop-custom :deep(.form-grid) { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 640px) {
  .source-choice, .source-fields, .case-meta { grid-template-columns: 1fr; }
  .modal-backdrop-custom :deep(.form-grid) { grid-template-columns: 1fr; }
  .modal-backdrop-custom :deep(.form-grid .span-2:first-child) { grid-column: auto; }
}
</style>
