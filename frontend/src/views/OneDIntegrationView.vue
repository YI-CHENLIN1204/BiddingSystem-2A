<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../services/api'

interface IntegrationStatus {
  configured: boolean
  mode: string
  sourceEndpoint: string
  message: string
}

interface MajorCategorySummary {
  majorCategory: string
  itemCount: number
  classifiedItemCount: number
  pendingItemCount: number
  hasDetectionRules: boolean
  detectionCategories: string[]
}

interface DetectionGroup {
  majorCategory: string
  detectionCategory: string
  itemCount: number
  units: string[]
  totalQuantity?: number | null
  aggregateUnit?: string | null
  aggregationStatus: string
  aggregationMessage: string
}

interface DetectionItem {
  sourceCategory?: string
  majorCategory: string
  detectionCategory?: string | null
  originalItemName: string
  itemCode?: string
  unit?: string
  quantity?: number
  oneDConfidence?: number
  classificationStatus: string
  matchedRule: string
}

interface DetectionPreview {
  oneDProjectId?: number
  sourceItemCount?: number
  totalItems: number
  majorCategoryCount: number
  classifiedItems: number
  pendingItems: number
  majorCategories: MajorCategorySummary[]
  groups: DetectionGroup[]
  items: DetectionItem[]
}

const status = ref<IntegrationStatus | null>(null)
const projectId = ref<number | null>(null)
const loadingStatus = ref(false)
const loadingPreview = ref(false)
const error = ref('')
const preview = ref<DetectionPreview | null>(null)
const sourceMode = ref<'demo' | 'oned'>('demo')

const classifiedRate = computed(() => {
  if (!preview.value?.totalItems) return 0
  return Math.round((preview.value.classifiedItems / preview.value.totalItems) * 100)
})

function formatNumber(value: number | null | undefined) {
  if (value === null || value === undefined || !Number.isFinite(Number(value))) return '-'
  return Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}

async function loadStatus() {
  loadingStatus.value = true
  error.value = ''

  try {
    status.value = await api<IntegrationStatus>('/one-d/status')
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法取得 1D 介接狀態'
  } finally {
    loadingStatus.value = false
  }
}

async function loadFromOneD() {
  error.value = ''
  preview.value = null

  if (!projectId.value || projectId.value <= 0) {
    error.value = '請輸入有效的 1D Project ID'
    return
  }

  loadingPreview.value = true
  sourceMode.value = 'oned'

  try {
    preview.value = await api<DetectionPreview>(
      `/one-d/projects/${projectId.value}/detection-preview`,
    )
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '1D 建案分類結果取得失敗'
  } finally {
    loadingPreview.value = false
  }
}

async function loadDemo() {
  error.value = ''
  preview.value = null
  loadingPreview.value = true
  sourceMode.value = 'demo'

  const request = {
    items: [
      {
        category: '住宅',
        chapter: '假設工程',
        code: 'TMP-001',
        name: '臨時水電及施工設施',
        unit: '式',
        quantity: 1,
        confidence: 0.97,
      },
      {
        category: '住宅',
        chapter: '土方及基礎工程',
        code: 'EAR-001',
        name: '地下室土方開挖',
        unit: 'M3',
        quantity: 8200,
        confidence: 0.96,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-001',
        name: 'SD420W高拉力鋼筋#4以上',
        unit: 'KG',
        quantity: 100000,
        confidence: 1,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-002',
        name: 'SD420W高拉力鋼筋#3以下',
        unit: 'KG',
        quantity: 40000,
        confidence: 0.99,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-003',
        name: '預拌混凝土 3500psi',
        unit: 'M3',
        quantity: 3200,
        confidence: 0.98,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-004',
        name: '結構體模板',
        unit: 'M2',
        quantity: 26000,
        confidence: 0.96,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-005',
        name: 'SD420W鋼筋續接器',
        unit: '個',
        quantity: 780,
        confidence: 0.93,
      },
      {
        category: '住宅',
        chapter: '鋼骨鋼構工程',
        code: 'STL-001',
        name: 'H型鋼鋼柱',
        unit: 'T',
        quantity: 850,
        confidence: 0.94,
      },
      {
        category: '住宅',
        chapter: '防水工程',
        code: 'WAT-001',
        name: '屋頂防水層',
        unit: 'M2',
        quantity: 1800,
        confidence: 0.95,
      },
      {
        category: '住宅',
        chapter: '門窗工程',
        code: 'DOR-001',
        name: '鋁門窗',
        unit: '樘',
        quantity: 126,
        confidence: 0.92,
      },
      {
        category: '住宅',
        chapter: '景觀工程',
        code: 'LND-001',
        name: '景觀植栽',
        unit: '式',
        quantity: 1,
        confidence: 0.91,
      },
    ],
  }

  try {
    preview.value = await api<DetectionPreview>('/one-d/detection-preview', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '示範建案分析失敗'
  } finally {
    loadingPreview.value = false
  }
}

onMounted(loadStatus)
</script>

<template>
  <div class="analysis-page">
    <h2 class="page-title">1D → 2A 建案資料流 POC</h2>
    <div class="page-desc">
      此頁僅為今天報告用的 POC，可視化 1D 與 2A 的預計資料流，不會寫入正式資料表，也不會改動既有異常偵測流程。
      POC 以「一個建案」為單位，模擬承接 1D 已完成的全部工程大項分類，再由 2A 往下建立偵錯細項。
      目前只示範結構體工程與鋼骨鋼構工程的細項，其餘工程大項完整保留。
    </div>

    <div v-if="error" class="message-box error">{{ error }}</div>

    <section class="analysis-card">
      <div class="card-header-wz">
        <div class="card-title-wz">1. POC：1D 建案資料來源</div>
        <div class="card-desc-wz">
          此 POC 模擬 2A 依 Project ID 取得 1D 已分類的整個建案；目前不會把資料寫進 2A 正式流程。
        </div>
      </div>

      <div class="card-body-wz">
        <div class="status-row">
          <span
            class="status-pill"
            :class="status?.configured ? 'status-ok' : 'status-warn'"
          >
            {{ loadingStatus ? '檢查中' : status?.configured ? '1D API 已設定' : '1D API 尚未設定' }}
          </span>

          <div class="status-text">
            <strong>{{ status?.message ?? '尚未取得狀態' }}</strong>
            <div v-if="status?.sourceEndpoint" class="small-text">
              來源：{{ status.sourceEndpoint }}｜模式：{{ status.mode }}
            </div>
          </div>

          <button type="button" class="btn btn-outline-secondary btn-sm" @click="loadStatus">
            重新檢查
          </button>
        </div>
      </div>
    </section>

    <section class="analysis-card">
      <div class="card-header-wz">
        <div class="card-title-wz">2. POC：載入示範建案</div>
        <div class="card-desc-wz">
          今天展示以內建示範建案為主；右側保留 1D Project ID 讀取能力，僅作為介接可行性驗證。
        </div>
      </div>

      <div class="card-body-wz">
        <div class="action-grid">
          <div class="demo-panel">
            <div class="panel-title">今天報告用示範建案</div>
            <div class="small-text">
              示範資料同時包含假設、土方及基礎、結構體、鋼骨鋼構、防水、門窗與景觀等 1D 工程大項，
              用來確認 2A 是以整個建案為單位，而不是只處理結構體工程。
            </div>
            <button
              type="button"
              class="btn-primary-wz"
              :disabled="loadingPreview"
              @click="loadDemo"
            >
              {{ loadingPreview && sourceMode === 'demo' ? '建立中' : '載入示範建案' }}
            </button>
          </div>

          <div class="oned-panel">
            <div class="panel-title">直接讀取 1D 建案</div>
            <div class="input-row">
              <input
                v-model.number="projectId"
                type="number"
                min="1"
                class="form-control"
                placeholder="1D Project ID"
              />
              <button
                type="button"
                class="btn-primary-wz"
                :disabled="loadingPreview || !status?.configured"
                @click="loadFromOneD"
              >
                {{ loadingPreview && sourceMode === 'oned' ? '讀取中' : '取得建案分類結果' }}
              </button>
            </div>
            <div class="small-text">
              此功能只用於驗證 1D API 可讀取；今天報告不需要真的連到 1D。
            </div>
          </div>
        </div>
      </div>
    </section>

    <template v-if="preview">
      <section class="summary-grid">
        <article class="summary-card">
          <div class="summary-label">1D 工程大項</div>
          <div class="summary-value">{{ preview.majorCategoryCount }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">建案工項總數</div>
          <div class="summary-value">{{ preview.totalItems }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">2A 已歸類細項</div>
          <div class="summary-value">{{ preview.classifiedItems }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">目前細項完成率</div>
          <div class="summary-value">{{ classifiedRate }}%</div>
        </article>
      </section>

      <section class="analysis-card">
        <div class="card-header-wz">
          <div class="card-title-wz">3. 建案工程大項總覽</div>
          <div class="card-desc-wz">
            下列工程大項全部來自 1D。2A 只標示目前已建立哪些偵錯細項規則；尚未建立規則的大項仍保留，不會被忽略或錯誤歸類。
          </div>
        </div>

        <div class="card-body-wz">
          <div class="major-grid">
            <article
              v-for="major in preview.majorCategories"
              :key="major.majorCategory"
              class="major-card"
              :class="major.hasDetectionRules ? 'major-ready' : 'major-pending'"
            >
              <div class="major-top">
                <div>
                  <div class="major-source">1D 工程大項</div>
                  <div class="major-name">{{ major.majorCategory }}</div>
                </div>
                <span
                  class="status-pill"
                  :class="major.hasDetectionRules ? 'status-ok' : 'status-warn'"
                >
                  {{ major.hasDetectionRules ? '已有 2A 細項' : '細項規則待建立' }}
                </span>
              </div>

              <div class="major-meta">
                {{ major.itemCount }} 筆工項
                <span v-if="major.classifiedItemCount">
                  ｜{{ major.classifiedItemCount }} 筆已歸類細項
                </span>
              </div>

              <div v-if="major.detectionCategories.length" class="category-tags">
                <span
                  v-for="category in major.detectionCategories"
                  :key="category"
                  class="category-tag"
                >
                  {{ category }}
                </span>
              </div>

              <div v-else class="major-note">
                目前保留 1D 大項資料，待確認此工程大項適合的數量偵錯細項後再建立規則。
              </div>
            </article>
          </div>
        </div>
      </section>

      <section class="analysis-card">
        <div class="card-header-wz">
          <div class="card-title-wz">4. 已建立的 2A 偵錯細項</div>
          <div class="card-desc-wz">
            工程大項由 1D 提供；下列材料類別才是 2A 目前新增的數量偵錯細項。單位一致時可先安全彙整，不一致時要求先做單位標準化。
          </div>
        </div>

        <div class="card-body-wz">
          <div v-if="preview.groups.length" class="group-grid">
            <article
              v-for="group in preview.groups"
              :key="`${group.majorCategory}-${group.detectionCategory}`"
              class="group-card"
            >
              <div class="group-major">{{ group.majorCategory }}</div>
              <div class="group-category">{{ group.detectionCategory }}</div>
              <div class="group-meta">
                {{ group.itemCount }} 筆原始工項
                <span v-if="group.units.length">｜原單位：{{ group.units.join('、') }}</span>
              </div>

              <div class="group-total" v-if="group.aggregationStatus === 'Ready'">
                彙整數量：{{ formatNumber(group.totalQuantity) }} {{ group.aggregateUnit }}
              </div>
              <div class="group-warning" v-else>
                {{ group.aggregationMessage }}
              </div>
            </article>
          </div>

          <div v-else class="empty-state">
            此建案目前沒有可套用的 2A 偵錯細項規則。
          </div>
        </div>
      </section>

      <section class="analysis-card">
        <div class="card-header-wz">
          <div class="card-title-wz">5. 建案工項追溯明細</div>
          <div class="card-desc-wz">
            每一筆都保留 1D 工程大項與原始工項名稱。已建立規則的工項會顯示 2A 偵錯細項；其他大項則清楚標示待建立規則。
          </div>
        </div>

        <div class="card-body-wz">
          <div class="result-wrapper">
            <table class="result-table">
              <thead>
                <tr>
                  <th>1D 工程大項</th>
                  <th>2A 偵錯細項</th>
                  <th>原始工項</th>
                  <th>單位</th>
                  <th>數量</th>
                  <th>2A 狀態</th>
                  <th>說明</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in preview.items" :key="index">
                  <td class="major-cell">{{ item.majorCategory }}</td>
                  <td class="material-cell">{{ item.detectionCategory ?? '—' }}</td>
                  <td class="material-cell">{{ item.originalItemName }}</td>
                  <td>{{ item.unit ?? '-' }}</td>
                  <td class="number-cell">{{ formatNumber(item.quantity) }}</td>
                  <td>
                    <span
                      class="status-pill"
                      :class="item.classificationStatus === 'Classified' ? 'status-ok' : 'status-warn'"
                    >
                      {{ item.classificationStatus === 'Classified' ? '細項已建立' : '待建立 / 確認' }}
                    </span>
                  </td>
                  <td class="rule-cell">{{ item.matchedRule }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.analysis-page { max-width: 96vw; margin: 0 auto; padding: 20px 0 40px; }
.page-title { font-size: 1.55rem; font-weight: 800; color: #1f2d3d; margin-bottom: 5px; }
.page-desc { font-size: 14px; color: #6c757d; margin-bottom: 18px; line-height: 1.75; }
.analysis-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; box-shadow: 0 3px 12px rgba(20,29,35,.06); margin-bottom: 18px; overflow: hidden; }
.card-header-wz { padding: 13px 17px; background: #f8f9fa; border-bottom: 1px solid #d8dbe0; }
.card-title-wz { font-size: 15px; font-weight: 800; color: #321fdb; }
.card-desc-wz { margin-top: 3px; color: #6c757d; font-size: 12px; line-height: 1.6; }
.card-body-wz { padding: 18px; }
.status-row { display: flex; align-items: center; gap: 12px; }
.status-text { flex: 1; min-width: 0; }
.small-text { color: #6c757d; font-size: 12px; margin-top: 5px; line-height: 1.6; }
.status-pill { display: inline-block; padding: 4px 9px; border-radius: 5px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.status-ok { color: #0f5132; background: #d1e7dd; }
.status-warn { color: #664d03; background: #fff3cd; }
.message-box { padding: 11px 14px; margin-bottom: 14px; border-radius: 6px; font-size: 13px; }
.message-box.error { color: #842029; background: #f8d7da; border: 1px solid #f5c2c7; }
.action-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.demo-panel, .oned-panel { border: 1px solid #d8dbe0; border-radius: 7px; padding: 16px; background: #fff; }
.panel-title { font-size: 14px; font-weight: 800; color: #1f2d3d; margin-bottom: 4px; }
.btn-primary-wz { border: 0; background: #321fdb; color: white; border-radius: 6px; padding: 9px 16px; font-weight: 700; margin-top: 12px; }
.btn-primary-wz:disabled { opacity: .55; cursor: not-allowed; }
.input-row { display: flex; gap: 10px; align-items: center; margin-top: 10px; }
.form-control { border-radius: 6px; border-color: #d8dbe0; font-size: 14px; }
.summary-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 18px; }
.summary-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; padding: 15px 17px; }
.summary-label { color: #6c757d; font-size: 12px; }
.summary-value { margin-top: 4px; font-size: 24px; font-weight: 800; color: #1f2d3d; }
.major-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }
.major-card { border: 1px solid #d8dbe0; border-radius: 8px; padding: 15px; background: #fff; }
.major-ready { border-left: 4px solid #198754; }
.major-pending { border-left: 4px solid #ffc107; }
.major-top { display: flex; justify-content: space-between; align-items: flex-start; gap: 10px; }
.major-source { font-size: 11px; color: #6c757d; }
.major-name { margin-top: 2px; font-size: 17px; font-weight: 800; color: #1f2d3d; }
.major-meta { margin-top: 9px; font-size: 12px; color: #6c757d; }
.category-tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 12px; }
.category-tag { padding: 4px 8px; border-radius: 999px; background: #e7f1ff; color: #084298; font-size: 12px; font-weight: 700; }
.major-note { margin-top: 12px; font-size: 12px; line-height: 1.55; color: #664d03; background: #fff8df; border-radius: 6px; padding: 8px 10px; }
.group-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
.group-card { border: 1px solid #d8dbe0; border-radius: 7px; padding: 14px; background: #fff; }
.group-major { color: #6c757d; font-size: 12px; }
.group-category { margin-top: 3px; font-size: 18px; font-weight: 800; color: #1f2d3d; }
.group-meta { margin-top: 8px; color: #6c757d; font-size: 12px; }
.group-total { margin-top: 10px; font-size: 13px; font-weight: 800; color: #0f5132; }
.group-warning { margin-top: 10px; font-size: 12px; line-height: 1.5; color: #664d03; background: #fff3cd; border-radius: 5px; padding: 7px 9px; }
.empty-state { padding: 24px; text-align: center; color: #6c757d; border: 1px dashed #ced4da; border-radius: 7px; }
.result-wrapper { overflow-x: auto; border: 1px solid #d8dbe0; border-radius: 7px; }
.result-table { width: 100%; min-width: 1100px; border-collapse: collapse; margin: 0; background: white; font-size: 13px; }
.result-table th { padding: 10px 8px; text-align: center; color: white; background: #5b6065; white-space: nowrap; font-weight: 700; }
.result-table td { padding: 10px 8px; border-bottom: 1px solid #e5e7eb; text-align: center; vertical-align: middle; }
.major-cell { font-weight: 800; color: #1f2d3d; }
.material-cell { text-align: left !important; font-weight: 700; }
.number-cell { text-align: right !important; font-variant-numeric: tabular-nums; }
.rule-cell { text-align: left !important; color: #495057; min-width: 300px; }
@media (max-width: 900px) {
  .action-grid, .summary-grid { grid-template-columns: 1fr; }
  .status-row, .input-row { align-items: stretch; flex-direction: column; }
}
</style>
