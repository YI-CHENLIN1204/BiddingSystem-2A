<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../services/api'

interface IntegrationStatus {
  configured: boolean
  mode: string
  sourceEndpoint: string
  message: string
}

interface DetectionGroup {
  majorCategory: string
  detectionCategory: string
  itemCount: number
  units: string[]
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
  classifiedItems: number
  pendingItems: number
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
    error.value = caught instanceof Error ? caught.message : '1D 分類結果取得失敗'
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
        chapter: '結構體工程',
        code: 'STR-001',
        name: 'SD420W高拉力鋼筋#4以上',
        unit: 'KG',
        quantity: 125000,
        confidence: 1,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-002',
        name: '預拌混凝土 3500psi',
        unit: 'M3',
        quantity: 3200,
        confidence: 0.98,
      },
      {
        category: '住宅',
        chapter: '結構體工程',
        code: 'STR-003',
        name: '結構體模板',
        unit: 'M2',
        quantity: 26000,
        confidence: 0.96,
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
        chapter: '結構體工程',
        code: 'STR-004',
        name: 'SD420W鋼筋續接器',
        unit: '個',
        quantity: 780,
        confidence: 0.93,
      },
    ],
  }

  try {
    preview.value = await api<DetectionPreview>('/one-d/detection-preview', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '示範資料分析失敗'
  } finally {
    loadingPreview.value = false
  }
}

onMounted(loadStatus)
</script>

<template>
  <div class="integration-page">
    <h2 class="page-title">1D / 2A 資料介接驗證</h2>
    <div class="page-desc">
      驗證 2A 可承接 1D 已完成的工程大項分類結果，再進一步整理為鋼筋、混凝土、模板、鋼骨等偵錯細項。
      本頁目前僅供 POC 驗證，不會修改 1D 資料，也不會寫入正式異常偵測結果。
    </div>

    <div v-if="error" class="message-box error">{{ error }}</div>

    <section class="integration-card">
      <div class="card-header-wz">
        <div class="card-title-wz">1. 1D API 介接狀態</div>
        <div class="card-desc-wz">2A 只讀取 1D 已儲存的分類結果，不要求 1D 重新跑分類。</div>
      </div>
      <div class="card-body-wz">
        <div class="status-row">
          <span
            class="status-pill"
            :class="status?.configured ? 'status-ok' : 'status-warn'"
          >
            {{ loadingStatus ? '檢查中' : status?.configured ? '已設定' : '尚未設定' }}
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

    <section class="integration-card">
      <div class="card-header-wz">
        <div class="card-title-wz">2. 執行細項分類預覽</div>
        <div class="card-desc-wz">
          可先用內建示範資料驗證分類規則；1D API 位址確認後，再輸入 Project ID 直接讀取 1D 結果。
        </div>
      </div>
      <div class="card-body-wz">
        <div class="action-grid">
          <div class="demo-panel">
            <div class="panel-title">本地 POC 示範</div>
            <div class="small-text">
              包含 SD420W 鋼筋、預拌混凝土、模板、H 型鋼，以及不應直接併入鋼筋重量的鋼筋續接器。
            </div>
            <button
              type="button"
              class="btn-primary-wz"
              :disabled="loadingPreview"
              @click="loadDemo"
            >
              {{ loadingPreview && sourceMode === 'demo' ? '分析中' : '載入示範資料' }}
            </button>
          </div>

          <div class="oned-panel">
            <div class="panel-title">直接讀取 1D</div>
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
                {{ loadingPreview && sourceMode === 'oned' ? '讀取中' : '取得 1D 結果' }}
              </button>
            </div>
            <div class="small-text">
              若按鈕無法使用，請先在 Docker 環境設定 ONE_D_API_BASE_URL。
            </div>
          </div>
        </div>
      </div>
    </section>

    <template v-if="preview">
      <section class="summary-grid">
        <article class="summary-card">
          <div class="summary-label">來源工項</div>
          <div class="summary-value">{{ preview.totalItems }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">已分類</div>
          <div class="summary-value">{{ preview.classifiedItems }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">待確認</div>
          <div class="summary-value">{{ preview.pendingItems }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">分類率</div>
          <div class="summary-value">{{ classifiedRate }}%</div>
        </article>
      </section>

      <section class="integration-card">
        <div class="card-header-wz">
          <div class="card-title-wz">3. 偵錯細項分類結果</div>
          <div class="card-desc-wz">
            保留 1D 工程大項，再由 2A 建立偵錯細項；原始工項仍完整保留供追溯。
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
                  <th>狀態</th>
                  <th>分類依據</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in preview.items" :key="index">
                  <td>{{ item.majorCategory }}</td>
                  <td class="material-cell">{{ item.detectionCategory ?? '待確認' }}</td>
                  <td class="material-cell">{{ item.originalItemName }}</td>
                  <td>{{ item.unit ?? '-' }}</td>
                  <td class="number-cell">{{ formatNumber(item.quantity) }}</td>
                  <td>
                    <span
                      class="status-pill"
                      :class="item.classificationStatus === 'Classified' ? 'status-ok' : 'status-warn'"
                    >
                      {{ item.classificationStatus === 'Classified' ? '已分類' : '待確認' }}
                    </span>
                  </td>
                  <td class="rule-cell">{{ item.matchedRule }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <section class="integration-card">
        <div class="card-header-wz">
          <div class="card-title-wz">4. 分類彙整</div>
          <div class="card-desc-wz">
            此階段只確認工項是否能穩定歸入偵錯細項；正式數量彙整仍需接單位換算與樣本數控制。
          </div>
        </div>
        <div class="card-body-wz">
          <div class="group-grid">
            <article v-for="group in preview.groups" :key="`${group.majorCategory}-${group.detectionCategory}`" class="group-card">
              <div class="group-major">{{ group.majorCategory }}</div>
              <div class="group-category">{{ group.detectionCategory }}</div>
              <div class="group-meta">
                {{ group.itemCount }} 筆工項
                <span v-if="group.units.length">｜單位：{{ group.units.join('、') }}</span>
              </div>
            </article>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.integration-page { max-width: 96vw; margin: 0 auto; padding: 20px 0 40px; }
.page-title { font-size: 1.55rem; font-weight: 800; color: #1f2d3d; margin-bottom: 5px; }
.page-desc { font-size: 14px; color: #6c757d; margin-bottom: 18px; line-height: 1.7; }
.integration-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; box-shadow: 0 3px 12px rgba(20,29,35,.06); margin-bottom: 18px; overflow: hidden; }
.card-header-wz { padding: 13px 17px; background: #f8f9fa; border-bottom: 1px solid #d8dbe0; }
.card-title-wz { font-size: 15px; font-weight: 800; color: #321fdb; }
.card-desc-wz { margin-top: 3px; color: #6c757d; font-size: 12px; }
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
.result-wrapper { overflow-x: auto; border: 1px solid #d8dbe0; border-radius: 7px; }
.result-table { width: 100%; min-width: 1000px; border-collapse: collapse; margin: 0; background: white; font-size: 13px; }
.result-table th { padding: 10px 8px; text-align: center; color: white; background: #5b6065; white-space: nowrap; font-weight: 700; }
.result-table td { padding: 10px 8px; border-bottom: 1px solid #e5e7eb; text-align: center; vertical-align: middle; }
.material-cell { text-align: left !important; font-weight: 700; }
.number-cell { text-align: right !important; font-variant-numeric: tabular-nums; }
.rule-cell { text-align: left !important; color: #495057; }
.group-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
.group-card { border: 1px solid #d8dbe0; border-radius: 7px; padding: 14px; background: #fff; }
.group-major { color: #6c757d; font-size: 12px; }
.group-category { margin-top: 3px; font-size: 18px; font-weight: 800; color: #1f2d3d; }
.group-meta { margin-top: 8px; color: #6c757d; font-size: 12px; }
@media (max-width: 900px) { .action-grid, .summary-grid { grid-template-columns: 1fr; } .status-row, .input-row { align-items: stretch; flex-direction: column; } }
</style>
