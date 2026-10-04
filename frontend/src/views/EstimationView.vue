<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api } from '../services/api'

interface EstimationResult {
  materialId: number
  materialName: string
  unitName: string
  hasEnoughSamples: boolean
  statusMessage: string
  referenceProjectCount: number
  minimumSampleCount: number
  lowerQuantity: number | null
  medianQuantity: number | null
  upperQuantity: number | null
}

interface EstimationResponse {
  targetProjectName: string
  candidateProjectCount: number
  requestedMaterialCount: number
  resultMaterialCount: number
  results: EstimationResult[]
}

const { options, error: optionsError } = useSystemOptions()
const target = ref({ projectType: '', structureType: '', city: '', totalFloorArea: null as number | null, floorCount: null as number | null, basementFloorCount: 0 as number | null, projectMonth: '' })
const scopeMode = ref<'core' | 'specified'>('core')
const selectedMaterials = ref<number[]>([])
const materialSearch = ref('')
const materialCategory = ref('')
const materialUnit = ref('')
const selectedOnly = ref(false)
const coreSearch = ref('')
const resultSearch = ref('')
const resultStatus = ref('')
const result = ref<EstimationResponse | null>(null)
const loading = ref(false)
const error = ref('')

const categories = computed(() => [...new Set(options.value.materials.map((item) => item.category || '未分類'))].sort((a, b) => a.localeCompare(b, 'zh-TW')))
const units = computed(() => [...new Set(options.value.materials.map((item) => item.unitName).filter(Boolean))].sort((a, b) => a.localeCompare(b, 'zh-TW')))
const filteredMaterials = computed(() => {
  const keyword = materialSearch.value.trim().toLocaleLowerCase('zh-TW')
  return options.value.materials.filter((item) => {
    const text = `${item.materialName} ${item.category || '未分類'} ${item.unitName}`.toLocaleLowerCase('zh-TW')
    return (!keyword || text.includes(keyword)) && (!materialCategory.value || (item.category || '未分類') === materialCategory.value) && (!materialUnit.value || item.unitName === materialUnit.value) && (!selectedOnly.value || selectedMaterials.value.includes(item.materialId))
  })
})
const coreMaterials = computed(() => {
  if (!target.value.projectType || !target.value.structureType) return []
  const keyword = coreSearch.value.trim().toLocaleLowerCase('zh-TW')
  const map = new Map<number, typeof options.value.coreMaterials[number]>()
  options.value.coreMaterials.forEach((item) => {
    if ((!item.projectType || item.projectType === target.value.projectType) && (!item.structureType || item.structureType === target.value.structureType)) map.set(item.materialId, item)
  })
  return [...map.values()].filter((item) => !keyword || `${item.materialName} ${item.category} ${item.unitName}`.toLocaleLowerCase('zh-TW').includes(keyword)).sort((a, b) => `${a.category}${a.materialName}`.localeCompare(`${b.category}${b.materialName}`, 'zh-TW'))
})
const filteredResults = computed(() => {
  const keyword = resultSearch.value.trim().toLocaleLowerCase('zh-TW')
  return (result.value?.results ?? []).filter((item) => (!keyword || item.materialName.toLocaleLowerCase('zh-TW').includes(keyword)) && (!resultStatus.value || (item.hasEnoughSamples ? 'enough' : 'insufficient') === resultStatus.value))
})

function buildTarget() {
  if (!target.value.projectType) throw new Error('請選擇建案類型')
  if (!target.value.structureType) throw new Error('請選擇結構類型')
  if (!target.value.city) throw new Error('請選擇縣市')
  if (!target.value.totalFloorArea || target.value.totalFloorArea <= 0) throw new Error('請輸入有效的總樓地板面積')
  if (!target.value.floorCount || target.value.floorCount <= 0) throw new Error('請輸入有效的地上樓層數')
  if (target.value.basementFloorCount === null || target.value.basementFloorCount < 0) throw new Error('地下樓層不可小於 0')
  if (!target.value.projectMonth) throw new Error('請選擇建案年月')
  return { targetMode: 1, ...target.value }
}

async function estimate() {
  error.value = ''
  try {
    const requestTarget = buildTarget()
    if (scopeMode.value === 'specified' && !selectedMaterials.value.length) throw new Error('請至少選擇一項材料')
    loading.value = true
    result.value = null
    result.value = scopeMode.value === 'core'
      ? await api<EstimationResponse>('/range-estimation/core', { method: 'POST', body: JSON.stringify(requestTarget) })
      : await api<EstimationResponse>('/range-estimation/batch', { method: 'POST', body: JSON.stringify({ target: requestTarget, materialIds: selectedMaterials.value }) })
    resultSearch.value = ''
    resultStatus.value = ''
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '估算失敗'
  } finally {
    loading.value = false
  }
}

function clear() {
  target.value = { projectType: '', structureType: '', city: '', totalFloorArea: null, floorCount: null, basementFloorCount: 0, projectMonth: '' }
  selectedMaterials.value = []
  materialSearch.value = ''; materialCategory.value = ''; materialUnit.value = ''; selectedOnly.value = false; coreSearch.value = ''; result.value = null; error.value = ''
}

function formatQuantity(value: number | null) {
  return value == null ? '—' : Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}
</script>

<template>
  <div class="estimate-page mt-4">
    <h2 class="page-title">工程用量估算</h2>
    <div class="page-desc">輸入目標建案條件，系統將依相似歷史建案與材料歷史用量，建立 P15 / P50 / P85 合理用量區間。</div>
    <div v-if="optionsError" class="message-panel error">{{ optionsError }}</div>

    <section class="estimate-card">
      <div class="card-header-wz"><div class="card-title-wz">輸入目標建案條件</div><div class="card-desc-wz">本次資料僅作為估算條件，不會建立新的歷史建案資料。</div></div>
      <div class="card-body-wz"><div class="row g-3">
        <div class="col-lg-2 col-md-4"><label class="form-label">建案類型</label><select v-model="target.projectType" class="form-select"><option value="">請選擇</option><option value="住宅">住宅</option><option value="廠房">廠房</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">結構類型</label><select v-model="target.structureType" class="form-select"><option value="">請選擇</option><option value="RC">RC</option><option value="SC">SC</option><option value="SRC">SRC</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">縣市</label><select v-model="target.city" class="form-select"><option value="">請選擇</option><option v-for="city in options.cities" :key="city">{{ city }}</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">總樓地板面積</label><div class="input-group"><input v-model.number="target.totalFloorArea" type="number" class="form-control" min="0" step=".01" placeholder="18000" /><span class="input-group-text">m²</span></div></div>
        <div class="col-lg-1 col-md-4"><label class="form-label">地上樓層</label><input v-model.number="target.floorCount" type="number" class="form-control" min="1" placeholder="20" /></div>
        <div class="col-lg-1 col-md-4"><label class="form-label">地下樓層</label><input v-model.number="target.basementFloorCount" type="number" class="form-control" min="0" placeholder="3" /></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">建案年月</label><input v-model="target.projectMonth" type="month" class="form-control" /></div>
      </div></div>
    </section>

    <section class="estimate-card">
      <div class="card-header-wz"><div class="card-title-wz">2. 選擇估算方式</div><div class="card-desc-wz">核心材料由參數管理中的設定自動決定；指定材料可自行選擇一項或多項材料。</div></div>
      <div class="card-body-wz">
        <div class="mode-grid mb-3"><div class="choice-card" :class="{ active: scopeMode === 'core' }" @click="scopeMode = 'core'"><div class="choice-title">核心材料快速估算</div><div class="choice-desc">依建案類型與結構，自動估算目前啟用的核心材料。</div></div><div class="choice-card" :class="{ active: scopeMode === 'specified' }" @click="scopeMode = 'specified'"><div class="choice-title">指定材料估算</div><div class="choice-desc">從標準材料中自行選擇一項或多項材料。</div></div></div>
        <div v-if="scopeMode === 'core'">
          <div class="material-preview-header"><div><div class="material-preview-title">待估算材料</div><div class="material-preview-desc">{{ target.projectType && target.structureType ? `${target.projectType} / ${target.structureType} 的核心估算材料` : '請先選擇建案類型與結構類型' }}</div></div><div class="material-search-box"><input v-model="coreSearch" class="form-control" placeholder="搜尋材料、分類或單位" /></div></div>
          <div class="material-preview-table-wrapper"><table class="material-preview-table"><thead><tr><th>材料名稱</th><th>分類</th><th>單位</th></tr></thead><tbody><tr v-if="!coreMaterials.length"><td colspan="3" class="preview-empty">{{ target.projectType && target.structureType ? '目前沒有符合條件的核心估算材料' : '請先選擇建案類型與結構類型' }}</td></tr><tr v-for="item in coreMaterials" :key="item.materialId"><td>{{ item.materialName }}</td><td>{{ item.category || '未分類' }}</td><td>{{ item.unitName }}</td></tr></tbody></table></div><div class="material-preview-count">目前共 <strong>{{ coreMaterials.length }}</strong> 項材料</div>
        </div>
        <div v-else>
          <div class="specified-search-row"><input v-model="materialSearch" type="search" class="form-control" placeholder="搜尋材料名稱" /><select v-model="materialCategory" class="form-select"><option value="">全部類別</option><option v-for="category in categories" :key="category">{{ category }}</option></select><select v-model="materialUnit" class="form-select"><option value="">全部單位</option><option v-for="unit in units" :key="unit">{{ unit }}</option></select><label class="selected-only-option"><input v-model="selectedOnly" type="checkbox" class="form-check-input" />只看已選材料</label></div>
          <div class="material-selector"><label v-for="item in filteredMaterials" :key="item.materialId" class="material-row"><input v-model="selectedMaterials" type="checkbox" class="form-check-input" :value="item.materialId" /><span class="material-main"><strong>{{ item.materialName }}</strong><span class="material-unit">{{ item.unitName }}</span></span><span class="material-category">{{ item.category || '未分類' }}</span></label><div v-if="!filteredMaterials.length" class="preview-empty">沒有符合篩選條件的材料</div></div>
          <div class="material-list-summary"><span>顯示 <strong>{{ filteredMaterials.length }}</strong> 項</span><span>已選擇 <strong>{{ selectedMaterials.length }}</strong> 項</span></div>
        </div>
      </div>
    </section>

    <section class="estimate-card"><div class="card-body-wz"><div v-if="error" class="message-panel error">{{ error }}</div><div class="action-bar"><button class="btn btn-outline-secondary" @click="clear">清除</button><button class="btn-analyze" :disabled="loading" @click="estimate">{{ loading ? '分析中...' : '開始估算' }}</button></div></div></section>

    <section v-if="loading || result" class="estimate-card">
      <div class="card-header-wz"><div class="result-header-actions"><div><div class="card-title-wz">3. 合理用量估算結果</div><div class="card-desc-wz">P15～P85 為目前模型建立的合理用量區間，P50 為加權分布中位數。</div></div><div class="estimation-filter-row"><input v-model="resultSearch" type="search" class="form-control" placeholder="搜尋材料名稱" /><select v-model="resultStatus" class="form-select"><option value="">全部狀態</option><option value="enough">可估算</option><option value="insufficient">樣本不足</option></select></div></div></div>
      <div class="card-body-wz"><div v-if="loading" class="loading-box">正在分析相似建案與材料歷史資料...</div><div v-else class="result-table-wrapper"><table class="result-table"><thead><tr><th>材料</th><th>有效參考建案</th><th>P15 下界</th><th>P50 中位</th><th>P85 上界</th><th>單位</th><th>樣本狀態</th></tr></thead><tbody><tr v-if="!filteredResults.length"><td colspan="7" class="empty-result">本次沒有可顯示的估算結果</td></tr><tr v-for="item in filteredResults" :key="item.materialId"><td class="material-name-cell">{{ item.materialName }}</td><td>{{ item.referenceProjectCount }} / {{ item.minimumSampleCount }} 案</td><td class="range-value">{{ formatQuantity(item.lowerQuantity) }}</td><td class="median-value">{{ formatQuantity(item.medianQuantity) }}</td><td class="range-value">{{ formatQuantity(item.upperQuantity) }}</td><td>{{ item.unitName }}</td><td :class="item.hasEnoughSamples ? 'status-ok' : 'status-insufficient'">{{ item.hasEnoughSamples ? '可估算' : item.statusMessage || '樣本不足' }}</td></tr></tbody></table></div></div>
    </section>
  </div>
</template>

<style scoped>
.estimate-page{max-width:96vw;margin:0 auto;padding-bottom:40px}.page-title{font-size:1.55rem;font-weight:800;margin-bottom:5px}.page-desc{color:#6c757d;font-size:14px;margin-bottom:18px}.estimate-card{background:#fff;border:1px solid #d8dbe0;border-radius:8px;box-shadow:0 3px 12px rgba(20,29,35,.06);margin-bottom:18px;overflow:hidden}.card-header-wz{background:#f8f9fa;border-bottom:1px solid #d8dbe0;padding:13px 17px}.card-title-wz{margin:0;color:#321fdb;font-size:15px;font-weight:800}.card-desc-wz{margin-top:3px;color:#6c757d;font-size:12px}.card-body-wz{padding:18px}.mode-grid{display:grid;grid-template-columns:repeat(2,minmax(250px,1fr));gap:14px}.choice-card{border:2px solid #e0e2e5;border-radius:8px;padding:15px;cursor:pointer;transition:.15s;background:#fff}.choice-card:hover{border-color:#aaa0f4}.choice-card.active{border-color:#321fdb;background:#f0edff}.choice-title{font-weight:800;margin-bottom:3px}.choice-desc{font-size:12px;color:#6c757d}.form-label{font-size:13px;font-weight:700}.form-control,.form-select{font-size:14px;border-radius:6px}.material-selector{border:1px solid #d8dbe0;border-radius:7px;max-height:300px;overflow-y:auto;background:#fff}.material-row{display:flex;align-items:center;padding:9px 12px;border-bottom:1px solid #eee;gap:10px}.material-row:hover{background:#f8f9fa}.material-category{margin-left:auto;color:#6c757d;font-size:11px}.material-main{display:flex;align-items:center;gap:7px}.material-unit{color:#6c757d;font-size:12px}.action-bar{display:flex;justify-content:flex-end;align-items:center;gap:10px}.btn-analyze{border:0;border-radius:6px;background:#321fdb;color:#fff;padding:9px 22px;font-weight:800}.btn-analyze:disabled{opacity:.65}.result-table-wrapper{overflow-x:auto}.result-table{width:100%;min-width:1050px;border-collapse:collapse;font-size:13px}.result-table th{background:#5b6065;color:#fff;padding:11px 8px;text-align:center;white-space:nowrap}.result-table td{padding:10px 8px;border-bottom:1px solid #e5e7eb;text-align:center;vertical-align:middle}.material-name-cell{text-align:left!important;font-weight:800}.range-value{font-variant-numeric:tabular-nums;font-weight:700}.median-value{color:#321fdb;font-weight:900}.status-ok{color:#2eb85c;font-weight:800}.status-insufficient{color:#f9b115;font-weight:800}.message-panel{border-radius:7px;padding:12px 15px;margin-bottom:15px;font-size:13px;font-weight:700}.message-panel.error{color:#842029;background:#f8d7da;border:1px solid #f5c2c7}.loading-box,.empty-result{text-align:center;padding:30px;color:#6c757d}.material-preview-header{display:flex;align-items:flex-end;justify-content:space-between;gap:16px;margin:18px 0 10px}.material-preview-title{font-size:14px;font-weight:800}.material-preview-desc{color:#6c757d;font-size:12px;margin-top:3px}.material-search-box{width:300px;max-width:100%}.material-preview-table-wrapper{border:1px solid #d8dbe0;border-radius:7px;overflow:hidden}.material-preview-table{width:100%;border-collapse:collapse;font-size:13px}.material-preview-table thead th{background:#f4f5f7;color:#343a40;padding:9px 12px;text-align:left;border-bottom:1px solid #d8dbe0}.material-preview-table tbody td{padding:9px 12px;border-bottom:1px solid #eceff1}.preview-empty{color:#6c757d;text-align:center!important;padding:22px!important}.material-preview-count{margin-top:8px;font-size:12px;color:#6c757d}.result-header-actions{display:flex;align-items:center;justify-content:space-between;gap:15px}.specified-search-row{display:grid;grid-template-columns:minmax(260px,1fr) 180px 160px auto;gap:10px;align-items:center;margin-bottom:10px}.selected-only-option{display:flex;align-items:center;gap:7px;white-space:nowrap;font-size:13px;cursor:pointer}.material-list-summary{display:flex;gap:18px;margin-top:8px;color:#6c757d;font-size:12px}.estimation-filter-row{display:grid;grid-template-columns:minmax(0,1fr) 140px;gap:10px;width:430px;max-width:100%}@media(max-width:800px){.specified-search-row,.mode-grid{grid-template-columns:1fr}.result-header-actions{align-items:stretch;flex-direction:column}.estimation-filter-row{width:100%;grid-template-columns:1fr}}
</style>
