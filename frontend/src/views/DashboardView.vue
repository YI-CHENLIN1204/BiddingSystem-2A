<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../services/api'

interface AnalysisCaseSummary {
  analysisCaseId: number
  caseName: string
  versionCount: number
  currentSourceType?: string | null
  updatedAt: string
}

interface ProjectSummary {
  projectId: number
  projectName: string
  hasFinalBid?: boolean
  finalBidItemCount?: number
  historicalTransactionCount?: number
  historyStatus?: string
}

interface ReadinessItem {
  detectionCategoryName: string
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

interface StructuralReadiness {
  majorCategory: string
  eligibleMetadataProjectCount: number
  availableProjectCount: number
  minimumSampleCount: number
  missingProjectCount: number
  status: string
  message: string
}

interface OneDStatus {
  configured: boolean
  message: string
}

const cases = ref<AnalysisCaseSummary[]>([])
const projects = ref<ProjectSummary[]>([])
const readiness = ref<ReadinessResponse | null>(null)
const structuralReadiness = ref<StructuralReadiness | null>(null)
const oneDStatus = ref<OneDStatus>({ configured: false, message: '1D API 尚未設定' })
const loading = ref(false)
const error = ref('')

const finalizedProjects = computed(() =>
  projects.value.filter(x => x.hasFinalBid),
)

const approvedHistoryProjectCount = computed(() =>
  projects.value.filter(x => x.historyStatus === 'Approved').length,
)

const historyProjectCount = computed(() =>
  projects.value.filter(x => (x.historicalTransactionCount ?? 0) > 0).length,
)

const totalRunsReady = computed(() =>
  readiness.value?.readyCount ?? 0,
)

function formatDate(value?: string | null) {
  return value ? new Date(value).toLocaleString('zh-TW') : '-'
}

function sourceLabel(value?: string | null) {
  if (value === 'OneD') return '1D 匯入'
  if (value === 'Upload') return '自行上傳'
  return '尚無版本'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [caseResult, projectResult, readinessResult, structuralResult, oneDResult] = await Promise.all([
      api<AnalysisCaseSummary[]>('/analysis-cases'),
      api<{ items: ProjectSummary[] }>('/projects'),
      api<ReadinessResponse>('/detection-category/readiness'),
      api<StructuralReadiness>('/major-category-price/readiness'),
      api<OneDStatus>('/one-d/status'),
    ])

    cases.value = caseResult
    projects.value = projectResult.items
    readiness.value = readinessResult
    structuralReadiness.value = structuralResult
    oneDStatus.value = oneDResult
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入系統總覽'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-stack dashboard-page">
    <div class="page-intro">
      <div>
        <h2>2A 系統總覽</h2>
        <p>目前流程分成「待偵錯案件」與「已定案歷史建案」兩條資料線，並以結構體工程、鋼筋、混凝土作為現階段分析範圍。</p>
      </div>
    </div>

    <div v-if="error" class="notice error">{{ error }}</div>
    <div v-if="loading" class="card p-4 text-muted">載入系統狀態中...</div>

    <template v-else>
      <section class="stat-grid">
        <RouterLink to="/analysis-cases" class="stat stat-link">
          <span>待偵錯案件</span>
          <strong>{{ cases.length }}</strong>
          <small>AnalysisCase</small>
        </RouterLink>

        <RouterLink to="/projects" class="stat stat-link">
          <span>已定案建案</span>
          <strong>{{ finalizedProjects.length }}</strong>
          <small>其中 {{ approvedHistoryProjectCount }} 案已發布為模型基準</small>
        </RouterLink>

        <div class="stat">
          <span>已形成數量歷史</span>
          <strong>{{ historyProjectCount }}</strong>
          <small>至少含鋼筋或混凝土</small>
        </div>

        <div class="stat">
          <span>可正式偵錯細項</span>
          <strong>{{ totalRunsReady }} / {{ readiness?.categoryCount ?? 0 }}</strong>
          <small>最低樣本數 {{ readiness?.minimumSampleCount ?? 5 }} 案</small>
        </div>
      </section>

      <section class="workflow-grid">
        <RouterLink to="/analysis-cases" class="workflow-card">
          <div class="workflow-number">01</div>
          <div>
            <h3>待偵錯案件</h3>
            <p>建立目前正在估價、修改中的案件；保留 V1、V2、V3 等版本與每次偵錯結果。</p>
            <div class="flow">
              <span>自行上傳</span>
              <span v-if="oneDStatus.configured">或 1D 匯入</span>
              <span>→ Version</span>
              <span>→ 偵錯</span>
            </div>
          </div>
        </RouterLink>

        <RouterLink to="/projects" class="workflow-card">
          <div class="workflow-number">02</div>
          <div>
            <h3>歷史建案</h3>
            <p>只保存已定案的正式最終標單，作為未來估算與異常判定的歷史基準。</p>
            <div class="flow">
              <span>最終標單</span>
              <span>→ 工項／材料整理</span>
              <span>→ 歷史基準</span>
            </div>
          </div>
        </RouterLink>

      </section>

      <section v-if="readiness" class="card">
        <div class="card-header">
          <div>
            <h3>模型歷史基準準備度</h3>
            <div class="text-muted small mt-1">結構體工程使用價格歷史；鋼筋與混凝土使用數量歷史。</div>
          </div>
        </div>

        <div class="readiness-grid">
          <div v-if="structuralReadiness" class="readiness-item">
            <div class="readiness-top">
              <strong>{{ structuralReadiness.majorCategory }}價格</strong>
              <span :class="structuralReadiness.status === 'ReadyForEstimation' ? 'ready' : 'waiting'">
                {{ structuralReadiness.status === 'ReadyForEstimation' ? '可估算' : '樣本不足' }}
              </span>
            </div>
            <div class="readiness-count">
              <strong>{{ structuralReadiness.availableProjectCount }}</strong>
              <span>/ {{ structuralReadiness.minimumSampleCount }} 案</span>
            </div>
            <div class="readiness-bar">
              <span :style="{ width: Math.min(100, structuralReadiness.availableProjectCount / structuralReadiness.minimumSampleCount * 100) + '%' }"></span>
            </div>
            <p>{{ structuralReadiness.message }}</p>
          </div>
          <div v-for="item in readiness.items" :key="item.detectionCategoryName" class="readiness-item">
            <div class="readiness-top">
              <strong>{{ item.detectionCategoryName }}</strong>
              <span :class="item.status === 'ReadyForEstimation' ? 'ready' : 'waiting'">
                {{ item.status === 'ReadyForEstimation' ? '可偵錯' : '樣本不足' }}
              </span>
            </div>

            <div class="readiness-count">
              <strong>{{ item.historicalProjectCount }}</strong>
              <span>/ {{ item.minimumSampleCount }} 案</span>
            </div>

            <div class="readiness-bar">
              <span :style="{ width: Math.min(100, item.historicalProjectCount / item.minimumSampleCount * 100) + '%' }"></span>
            </div>

            <p>{{ item.message }}</p>
          </div>
        </div>
      </section>

      <section class="split-grid">
        <div class="card">
          <div class="card-header">
            <div>
              <h3>最近待偵錯案件</h3>
              <div class="text-muted small mt-1">顯示最近更新的案件。</div>
            </div>
            <RouterLink to="/analysis-cases" class="small-link">全部案件 →</RouterLink>
          </div>
          <div v-if="!cases.length" class="empty-state">目前沒有待偵錯案件。</div>
          <div v-else class="recent-list">
            <RouterLink
              v-for="item in [...cases].sort((a,b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()).slice(0,5)"
              :key="item.analysisCaseId"
              :to="`/analysis-cases/${item.analysisCaseId}`"
              class="recent-row"
            >
              <div>
                <strong>{{ item.caseName }}</strong>
                <span>{{ sourceLabel(item.currentSourceType) }} ・ {{ item.versionCount }} 個版本</span>
              </div>
              <small>{{ formatDate(item.updatedAt) }}</small>
            </RouterLink>
          </div>
        </div>

        <div class="card environment-card">
          <div class="card-header">
            <div>
              <h3>資料來源環境</h3>
              <div class="text-muted small mt-1">2A 核心功能不依賴 1D。</div>
            </div>
          </div>
          <div class="environment-body">
            <div class="environment-status">
              <span class="status-dot" :class="{ online: oneDStatus.configured }"></span>
              <div>
                <strong>{{ oneDStatus.configured ? '已連接 1D' : '獨立 2A 模式' }}</strong>
                <p>{{ oneDStatus.configured ? '產學環境可額外從 1D 匯入待偵錯案件。' : '目前沒有 1D，直接使用自行上傳即可完整操作 2A。' }}</p>
              </div>
            </div>
            <div class="scope-box">
              <span>大項估算</span><strong>結構體工程</strong>
              <span>細項偵錯</span><strong>鋼筋、混凝土</strong>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.dashboard-page { max-width: 1500px; margin: 0 auto; }
.stat-link { color: inherit; text-decoration: none; }
.stat small { display: block; margin-top: 5px; color: var(--muted); font-size: 11px; }
.workflow-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
.workflow-card { display: flex; gap: 16px; padding: 20px; color: inherit; text-decoration: none; background: #fff; border: 1px solid var(--border); border-radius: 8px; box-shadow: var(--shadow); }
.workflow-card:hover { border-color: #aeb7c5; }
.workflow-number { flex: 0 0 auto; color: #a4adba; font-size: 24px; font-weight: 900; }
.workflow-card h3 { margin: 1px 0 7px; font-size: 17px; }
.workflow-card p { min-height: 44px; margin: 0; color: var(--muted); font-size: 12px; line-height: 1.55; }
.flow { display: flex; flex-wrap: wrap; gap: 5px; margin-top: 14px; }
.flow span { padding: 4px 7px; color: #45536c; background: #f1f3f6; border-radius: 5px; font-size: 11px; font-weight: 700; }
.card-header { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.card-header h3 { margin: 0; font-size: 16px; }
.readiness-grid { display: grid; grid-template-columns: repeat(3, 1fr); }
.readiness-item { padding: 20px; border-right: 1px solid #edf0f4; }
.readiness-item:last-child { border-right: 0; }
.readiness-top { display: flex; justify-content: space-between; align-items: center; }
.readiness-top > span { padding: 4px 7px; border-radius: 999px; font-size: 11px; font-weight: 800; }
.readiness-top .ready { color: #176e50; background: #e8f8f1; }
.readiness-top .waiting { color: #8a6511; background: #fff5d9; }
.readiness-count { display: flex; align-items: baseline; gap: 6px; margin-top: 12px; }
.readiness-count strong { font-size: 26px; }
.readiness-count span { color: var(--muted); font-size: 13px; }
.readiness-bar { height: 7px; margin: 10px 0; overflow: hidden; background: #edf0f4; border-radius: 999px; }
.readiness-bar span { display: block; height: 100%; background: var(--wz-primary); border-radius: inherit; }
.readiness-item p { margin: 0; color: var(--muted); font-size: 12px; }
.split-grid { display: grid; grid-template-columns: 1.35fr .65fr; gap: 16px; }
.small-link { color: var(--wz-primary); text-decoration: none; font-size: 12px; font-weight: 700; }
.recent-list { display: grid; }
.recent-row { display: flex; justify-content: space-between; align-items: center; gap: 14px; padding: 13px 18px; color: inherit; text-decoration: none; border-bottom: 1px solid #edf0f4; }
.recent-row:last-child { border-bottom: 0; }
.recent-row:hover { background: #fafbfc; }
.recent-row strong, .recent-row span { display: block; }
.recent-row span, .recent-row small { margin-top: 3px; color: var(--muted); font-size: 11px; }
.environment-body { padding: 18px; }
.environment-status { display: flex; gap: 10px; align-items: flex-start; }
.status-dot { width: 10px; height: 10px; margin-top: 5px; background: #9aa3ad; border-radius: 50%; }
.status-dot.online { background: var(--wz-success); }
.environment-status p { margin: 4px 0 0; color: var(--muted); font-size: 12px; line-height: 1.5; }
.scope-box { display: grid; grid-template-columns: 88px 1fr; gap: 8px 10px; margin-top: 18px; padding-top: 16px; border-top: 1px solid #edf0f4; }
.scope-box span { color: var(--muted); font-size: 12px; }
.scope-box strong { font-size: 13px; }
@media (max-width: 1000px) {
  .workflow-grid, .split-grid { grid-template-columns: 1fr; }
}
@media (max-width: 700px) {
  .readiness-grid { grid-template-columns: 1fr; }
  .readiness-item { border-right: 0; border-bottom: 1px solid #edf0f4; }
  .readiness-item:last-child { border-bottom: 0; }
}
</style>
