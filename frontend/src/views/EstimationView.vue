<script setup lang="ts">
import { ref } from 'vue'
import ProjectFields from '../components/ProjectFields.vue'
import MaterialCapabilityPanel from '../components/MaterialCapabilityPanel.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api } from '../services/api'
import type { ProjectForm } from '../types'

interface PriceReference {
  projectId: number
  projectName: string
  projectDate: string
  floorAreaM2: number
  floorAreaPing: number
  totalAmount: number
  pricePerPing: number
  similarityScore: number
  ageYears: number
  finalWeight: number
}

interface StructuralEstimationResult {
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
  analysisStatus: string
  trafficLight: string
  references: PriceReference[]
}

const { options, error: optionsError } = useSystemOptions()
const form = ref<ProjectForm>(emptyForm())
const topCount = ref<number | null>(null)
const result = ref<StructuralEstimationResult | null>(null)
const loading = ref(false)
const error = ref('')

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

function validate() {
  if (!form.value.projectType) throw new Error('請選擇建案類型')
  if (!form.value.structureType) throw new Error('請選擇結構類型')
  if (!form.value.city) throw new Error('請選擇縣市')
  if (!form.value.totalFloorArea || form.value.totalFloorArea <= 0) throw new Error('請輸入有效的總樓地板面積')
  if (!form.value.siteArea || form.value.siteArea <= 0) throw new Error('請輸入有效的基地面積')
  if (!form.value.floorCount || form.value.floorCount <= 0) throw new Error('請輸入有效的地上樓層')
  if (form.value.basementFloorCount == null || form.value.basementFloorCount < 0) throw new Error('請輸入有效的地下樓層')
  if (!form.value.projectMonth) throw new Error('請選擇建案年月')
  if (topCount.value != null && topCount.value <= 0) throw new Error('Top K 必須大於 0')
}

async function estimate() {
  loading.value = true
  error.value = ''
  result.value = null

  try {
    validate()

    result.value = await api<StructuralEstimationResult>('/major-category-price/range', {
      method: 'POST',
      body: JSON.stringify({
        target: {
          projectType: form.value.projectType,
          structureType: form.value.structureType,
          city: form.value.city,
          totalFloorArea: form.value.totalFloorArea,
          siteArea: form.value.siteArea,
          floorCount: form.value.floorCount,
          basementFloorCount: form.value.basementFloorCount,
          projectDate: `${form.value.projectMonth}-01`,
        },
        majorCategory: '結構體工程',
        observedAmount: null,
        topCount: topCount.value || null,
      }),
    })
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '結構體工程估算失敗'
  } finally {
    loading.value = false
  }
}

function clear() {
  form.value = emptyForm()
  topCount.value = null
  result.value = null
  error.value = ''
}

function formatNumber(value: number | null | undefined, digits = 2) {
  if (value == null) return '—'
  return Number(value).toLocaleString('zh-TW', { maximumFractionDigits: digits })
}

function formatMonth(value?: string | null) {
  return value ? value.slice(0, 7) : '—'
}
</script>

<template>
  <div class="page-stack structural-estimation">
    <div class="page-intro">
      <div>
        <h2>工程與材料估算</h2>
        <p>輸入建案條件，以相似歷史建案的「結構體工程每坪單價」建立 P15 / P50 / P85 合理價格區間。</p>
      </div>
    </div>

    <div v-if="optionsError || error" class="notice error">{{ optionsError || error }}</div>

    <MaterialCapabilityPanel :structure-type="form.structureType" allow-specification-estimate />

    <section class="notice info">
      <strong>目前估算單位：結構體工程。</strong>
      鋼筋與混凝土屬於細項數量偵錯，不在這個頁面分別估價。這裡只建立結構體工程整體價格合理範圍。
    </section>

    <section class="card">
      <div class="card-header">
        <div>
          <h3>目標建案條件</h3>
          <div class="text-muted small mt-1">此處只做範圍估算，不建立待偵錯案件，也不寫入歷史資料。</div>
        </div>
      </div>
      <div class="card-body">
        <ProjectFields
          v-model="form"
          :options="options"
          area-label="總樓地板面積（m²）"
          floor-label="地上樓層"
          basement-floor-label="地下樓層"
        />

        <div class="estimate-options mt-4">
          <label class="field">
            <span>參考 Top K（選填）</span>
            <input v-model.number="topCount" type="number" min="1" placeholder="留空使用系統預設" />
          </label>
          <div class="action-buttons">
            <button type="button" class="btn btn-outline-secondary" :disabled="loading" @click="clear">清除</button>
            <button type="button" class="btn btn-primary" :disabled="loading" @click="estimate">
              {{ loading ? '估算中...' : '開始估算結構體工程' }}
            </button>
          </div>
        </div>
      </div>
    </section>

    <section v-if="result" class="card result-card">
      <div class="card-header result-head">
        <div>
          <h3>{{ result.majorCategory }}估算結果</h3>
          <div class="text-muted small mt-1">{{ result.statusMessage }}</div>
        </div>
        <span
          class="status-badge"
          :class="result.hasEnoughSamples ? 'ready' : 'insufficient'"
        >
          {{ result.hasEnoughSamples ? '可估算' : '樣本不足' }}
        </span>
      </div>

      <div class="summary-grid">
        <div>
          <span>候選相似建案</span>
          <strong>{{ result.candidateProjectCount }}</strong>
        </div>
        <div>
          <span>有結構體價格資料</span>
          <strong>{{ result.availableProjectCount }}</strong>
        </div>
        <div>
          <span>本次有效參考</span>
          <strong>{{ result.referenceProjectCount }}</strong>
        </div>
      </div>

      <div class="range-section">
        <div class="range-title">每坪單價合理區間</div>
        <div class="range-grid">
          <div>
            <span>P15 下界</span>
            <strong>{{ formatNumber(result.lowerPricePerPing) }}</strong>
            <small>元 / 坪</small>
          </div>
          <div class="median">
            <span>P50 中位</span>
            <strong>{{ formatNumber(result.medianPricePerPing) }}</strong>
            <small>元 / 坪</small>
          </div>
          <div>
            <span>P85 上界</span>
            <strong>{{ formatNumber(result.upperPricePerPing) }}</strong>
            <small>元 / 坪</small>
          </div>
        </div>
      </div>

      <div class="range-section">
        <div class="range-title">依本案樓地板面積換算總價區間</div>
        <div class="range-grid">
          <div>
            <span>P15 下界</span>
            <strong>{{ formatNumber(result.lowerAmount) }}</strong>
            <small>元</small>
          </div>
          <div class="median">
            <span>P50 中位</span>
            <strong>{{ formatNumber(result.medianAmount) }}</strong>
            <small>元</small>
          </div>
          <div>
            <span>P85 上界</span>
            <strong>{{ formatNumber(result.upperAmount) }}</strong>
            <small>元</small>
          </div>
        </div>
      </div>

      <div class="reference-section">
        <div class="reference-title">本次參考建案</div>
        <div v-if="!result.references?.length" class="empty-state">
          目前沒有可用的結構體工程歷史價格資料。
        </div>
        <div v-else class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>建案</th>
                <th>年月</th>
                <th>樓地板面積</th>
                <th>結構體總價</th>
                <th>每坪單價</th>
                <th>相似度</th>
                <th>資料年齡</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in result.references" :key="item.projectId">
                <td><strong>{{ item.projectName }}</strong></td>
                <td>{{ formatMonth(item.projectDate) }}</td>
                <td class="number">{{ formatNumber(item.floorAreaM2) }} m²</td>
                <td class="number">{{ formatNumber(item.totalAmount) }}</td>
                <td class="number">{{ formatNumber(item.pricePerPing) }}</td>
                <td class="number">{{ formatNumber(item.similarityScore) }}%</td>
                <td class="number">{{ formatNumber(item.ageYears) }} 年</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.structural-estimation { max-width: 1500px; margin: 0 auto; }
.card-header h3 { margin: 0; font-size: 16px; }
.estimate-options { display: flex; align-items: end; justify-content: space-between; gap: 20px; }
.estimate-options .field { width: 240px; }
.action-buttons { display: flex; gap: 8px; }
.result-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; }
.status-badge { padding: 6px 10px; border-radius: 999px; font-size: 12px; font-weight: 800; }
.status-badge.ready { color: #176e50; background: #e8f8f1; }
.status-badge.insufficient { color: #8a6511; background: #fff5d9; }
.summary-grid { display: grid; grid-template-columns: repeat(3, 1fr); border-bottom: 1px solid #edf0f4; }
.summary-grid div { padding: 18px; border-right: 1px solid #edf0f4; }
.summary-grid div:last-child { border-right: 0; }
.summary-grid span, .range-grid span { display: block; color: var(--muted); font-size: 12px; }
.summary-grid strong { display: block; margin-top: 5px; font-size: 22px; }
.range-section { padding: 20px; border-bottom: 1px solid #edf0f4; }
.range-title, .reference-title { margin-bottom: 12px; font-weight: 800; }
.range-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.range-grid > div { padding: 16px; border: 1px solid #e2e6ec; border-radius: 8px; background: #fff; }
.range-grid > div.median { border-color: var(--wz-primary); background: var(--blue-soft); }
.range-grid strong { display: block; margin-top: 7px; font-size: 23px; font-variant-numeric: tabular-nums; }
.range-grid small { color: var(--muted); }
.reference-section { padding: 20px; }
.number { text-align: right; font-variant-numeric: tabular-nums; }
@media (max-width: 800px) {
  .estimate-options { align-items: stretch; flex-direction: column; }
  .estimate-options .field { width: 100%; }
  .action-buttons { justify-content: flex-end; }
  .summary-grid, .range-grid { grid-template-columns: 1fr; }
  .summary-grid div { border-right: 0; border-bottom: 1px solid #edf0f4; }
  .summary-grid div:last-child { border-bottom: 0; }
}
</style>
