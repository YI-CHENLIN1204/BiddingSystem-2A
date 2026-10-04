<script setup lang="ts">
import { computed, ref } from 'vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { apiForm } from '../services/api'

interface AnomalyResult {
  materialId: number
  materialName: string
  unitName: string
  actualQuantity: number
  sourceRowCount: number
  referenceProjectCount: number
  minimumSampleCount: number
  lowerQuantity: number | null
  medianQuantity: number | null
  upperQuantity: number | null
  anomalyStatus: string
  statusMessage: string
}

interface UnmappedItem {
  sheetName?: string
  rowNumber?: number
  itemName: string
  spec?: string
  unit?: string
  quantity?: number
  reason: string
}

interface AnalysisResponse {
  fileName: string
  results: AnomalyResult[]
  unmappedItems: UnmappedItem[]
}

const { options, error: optionsError } = useSystemOptions()
const project = ref({
  projectType: '', structureType: '', city: '', totalFloorArea: null as number | null,
  floorCount: null as number | null, basementFloorCount: null as number | null, projectMonth: '',
})
const file = ref<File | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)
const loading = ref(false)
const error = ref('')
const result = ref<AnalysisResponse | null>(null)
const search = ref('')
const status = ref('')
const showProblems = ref(false)

const filteredResults = computed(() => {
  const keyword = search.value.trim().toLocaleLowerCase('zh-TW')
  return (result.value?.results ?? []).filter((item) => {
    const matchesKeyword = !keyword || `${item.materialName} ${item.statusMessage}`.toLocaleLowerCase('zh-TW').includes(keyword)
    return matchesKeyword && (!status.value || item.anomalyStatus === status.value)
  })
})

function selectFile(event: Event) {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
}

function formatNumber(value: number | null | undefined, nullText = '樣本不足') {
  if (value === null || value === undefined || !Number.isFinite(Number(value))) return nullText
  return Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}

function statusLabel(value: string) {
  return ({ High: '偏高', Low: '偏低', Normal: '正常', InsufficientSamples: '樣本不足' } as Record<string, string>)[value] ?? '無法判定'
}

function validate() {
  if (!project.value.projectType) throw new Error('請選擇建案類型')
  if (!project.value.structureType) throw new Error('請選擇結構類型')
  if (!project.value.city) throw new Error('請選擇縣市')
  if (!project.value.totalFloorArea || project.value.totalFloorArea <= 0) throw new Error('請輸入有效的總樓地板面積')
  if (!project.value.floorCount || project.value.floorCount <= 0) throw new Error('請輸入有效的地上樓層數')
  if (project.value.basementFloorCount === null || project.value.basementFloorCount < 0) throw new Error('請輸入有效的地下樓層數')
  if (!project.value.projectMonth) throw new Error('請選擇建案年月')
  if (!file.value) throw new Error('請選擇待檢 Excel 標單')
  if (!['.xlsx', '.xls', '.xlsm', '.xlsb', '.csv'].some((extension) => file.value!.name.toLowerCase().endsWith(extension))) {
    throw new Error('支援格式：.xlsx、.xls、.xlsm、.xlsb、.csv')
  }
}

async function analyze() {
  error.value = ''
  try {
    validate()
    loading.value = true
    result.value = null
    const form = new FormData()
    form.append('excelFile', file.value!)
    Object.entries(project.value).forEach(([key, value]) => form.append(key, String(value)))
    result.value = await apiForm<AnalysisResponse>('/bid-anomaly/analyze', form)
    search.value = ''
    status.value = ''
    showProblems.value = false
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '異常分析失敗'
  } finally {
    loading.value = false
  }
}

function clear() {
  project.value = { projectType: '', structureType: '', city: '', totalFloorArea: null, floorCount: null, basementFloorCount: null, projectMonth: '' }
  file.value = null
  if (fileInput.value) fileInput.value.value = ''
  result.value = null
  search.value = ''
  status.value = ''
  error.value = ''
  showProblems.value = false
}
</script>

<template>
  <div class="anomaly-page">
    <h2 class="page-title">標單異常偵測</h2>
    <div class="page-desc">輸入目標建案條件並上傳待檢標單，系統將依相似歷史建案建立材料合理用量區間，再與標單實際用量進行比較。</div>

    <div v-if="optionsError" class="message-box error">{{ optionsError }}</div>

    <section class="anomaly-card">
      <div class="card-header-wz"><div class="card-title-wz">1. 目標建案條件</div><div class="card-desc-wz">建案條件只供本次分析使用，不會建立新的歷史建案資料。</div></div>
      <div class="card-body-wz"><div class="row g-3">
        <div class="col-lg-2 col-md-4"><label class="form-label">建案類型</label><select v-model="project.projectType" class="form-select"><option value="">請選擇</option><option value="住宅">住宅</option><option value="廠房">廠房</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">結構類型</label><select v-model="project.structureType" class="form-select"><option value="">請選擇</option><option value="RC">RC</option><option value="SC">SC</option><option value="SRC">SRC</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">縣市</label><select v-model="project.city" class="form-select"><option value="">請選擇</option><option v-for="city in options.cities" :key="city" :value="city">{{ city }}</option></select></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">總樓地板面積</label><div class="input-group"><input v-model.number="project.totalFloorArea" type="number" min="0" step="0.01" class="form-control" placeholder="例如 18000" /><span class="input-group-text">m²</span></div></div>
        <div class="col-lg-1 col-md-4"><label class="form-label">地上樓層</label><input v-model.number="project.floorCount" type="number" min="1" class="form-control" placeholder="20" /></div>
        <div class="col-lg-1 col-md-4"><label class="form-label">地下樓層</label><input v-model.number="project.basementFloorCount" type="number" min="0" class="form-control" placeholder="3" /></div>
        <div class="col-lg-2 col-md-4"><label class="form-label">建案年月</label><input v-model="project.projectMonth" type="month" class="form-control" /></div>
      </div></div>
    </section>

    <section class="anomaly-card">
      <div class="card-header-wz"><div class="card-title-wz">2. 上傳待檢標單</div><div class="card-desc-wz">支援 .xlsx、.xls、.xlsm、.xlsb、.csv 格式；上傳檔案只供本次分析，不會寫入歷史資料。</div></div>
      <div class="card-body-wz"><div class="file-area">
        <input ref="fileInput" type="file" id="excelFile" class="file-input-native" accept=".xlsx,.xls,.xlsm,.xlsb,.csv" @change="selectFile" />
        <label for="excelFile" class="file-picker"><span class="file-picker-button">選擇 Excel 檔案</span><span class="file-picker-name" :class="{ 'has-file': file }">{{ file?.name ?? '尚未選擇檔案' }}</span></label>
      </div></div>
    </section>

    <section class="anomaly-card"><div class="card-body-wz">
      <div v-if="error" class="message-box error">{{ error }}</div>
      <div class="action-row"><button type="button" class="btn btn-outline-secondary" @click="clear">清除</button><button type="button" class="btn-analyze" :disabled="loading" @click="analyze">{{ loading ? '分析中' : '開始異常分析' }}</button></div>
    </div></section>

    <section v-if="loading || result" class="anomaly-card">
      <div class="card-header-wz"><div class="card-title-wz">3. 分析結果</div><div class="card-desc-wz">{{ result?.fileName ? `檔案：${result.fileName}` : '' }}</div></div>
      <div class="card-body-wz">
        <div v-if="loading" class="loading-box show">系統正在解析標單並計算合理用量區間，請稍候。</div>
        <template v-else-if="result">
          <div class="result-toolbar"><div class="result-toolbar-text"><div class="section-title">材料異常分析</div><div class="result-note">實際數量低於 P15 判定為偏低，高於 P85 判定為偏高；歷史樣本不足時不進行異常判定。</div></div><div class="result-toolbar-search"><div class="anomaly-filter-row"><input v-model="search" type="search" class="form-control" placeholder="搜尋材料名稱" /><select v-model="status" class="form-select"><option value="">全部判定</option><option value="High">偏高</option><option value="Low">偏低</option><option value="Normal">正常</option><option value="InsufficientSamples">樣本不足</option></select></div></div></div>
          <div class="result-wrapper"><table class="result-table"><thead><tr><th>材料</th><th>實際數量</th><th>P15 下界</th><th>P50 中位</th><th>P85 上界</th><th>單位</th><th>來源工項</th><th>參考樣本</th><th>判定</th><th>說明</th></tr></thead><tbody>
            <tr v-if="!filteredResults.length"><td colspan="10" class="empty-row">本次沒有可進行合理區間比較的材料</td></tr>
            <tr v-for="item in filteredResults" :key="item.materialId"><td class="material-cell">{{ item.materialName }}</td><td class="number-cell">{{ formatNumber(item.actualQuantity, '未提供') }}</td><td class="number-cell">{{ formatNumber(item.lowerQuantity) }}</td><td class="number-cell">{{ formatNumber(item.medianQuantity) }}</td><td class="number-cell">{{ formatNumber(item.upperQuantity) }}</td><td>{{ item.unitName }}</td><td>{{ item.sourceRowCount }}</td><td>{{ item.referenceProjectCount }} / {{ item.minimumSampleCount }}</td><td><span class="status-badge" :class="`status-${item.anomalyStatus === 'InsufficientSamples' ? 'insufficient' : item.anomalyStatus.toLowerCase()}`">{{ statusLabel(item.anomalyStatus) }}</span></td><td class="material-cell">{{ item.statusMessage }}</td></tr>
          </tbody></table></div>
        </template>
      </div>
    </section>

    <section v-if="result?.unmappedItems?.length" class="anomaly-card">
      <div class="card-header-wz problem-header"><div><div class="card-title-wz">4. 未對應與無效工項（{{ result.unmappedItems.length }} 筆）</div><div class="card-desc-wz">這些工項未進入材料合理區間比較，需要時再查看明細。</div></div><button type="button" class="btn btn-outline-secondary btn-sm" @click="showProblems = !showProblems">{{ showProblems ? '收合明細' : '查看明細' }}</button></div>
      <div v-if="showProblems" class="card-body-wz"><div class="result-wrapper"><table class="result-table"><thead><tr><th>工作表</th><th>列號</th><th>項目名稱</th><th>規格</th><th>原單位</th><th>原數量</th><th>原因</th></tr></thead><tbody><tr v-for="(item, index) in result.unmappedItems" :key="index"><td>{{ item.sheetName }}</td><td>{{ item.rowNumber }}</td><td class="material-cell">{{ item.itemName }}</td><td class="material-cell">{{ item.spec }}</td><td>{{ item.unit }}</td><td class="number-cell">{{ formatNumber(item.quantity) }}</td><td class="material-cell">{{ item.reason }}</td></tr></tbody></table></div></div>
    </section>
  </div>
</template>

<style scoped>
.anomaly-page { max-width: 96vw; margin: 0 auto; padding: 20px 0 40px; }
.page-title { font-size: 1.55rem; font-weight: 800; color: #1f2d3d; margin-bottom: 5px; }
.page-desc { font-size: 14px; color: #6c757d; margin-bottom: 18px; }
.anomaly-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; box-shadow: 0 3px 12px rgba(20,29,35,.06); margin-bottom: 18px; overflow: hidden; }
.card-header-wz { padding: 13px 17px; background: #f8f9fa; border-bottom: 1px solid #d8dbe0; }
.card-title-wz { font-size: 15px; font-weight: 800; color: #321fdb; margin: 0; }
.card-desc-wz { margin-top: 3px; color: #6c757d; font-size: 12px; }
.card-body-wz { padding: 18px; }
.form-label { font-size: 13px; font-weight: 700; color: #333; }
.form-control, .form-select { border-radius: 6px; border-color: #d8dbe0; font-size: 14px; }
.file-area { display: flex; align-items: stretch; gap: 12px; }
.file-input-native { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; }
.file-picker { flex: 1; height: 44px; border: 1px solid #d8dbe0; border-radius: 6px; display: flex; align-items: center; background: white; overflow: hidden; cursor: pointer; }
.file-picker:hover { border-color: #321fdb; }
.file-picker-button { height: 100%; padding: 0 18px; display: flex; align-items: center; background: #f0f2ff; color: #321fdb; border-right: 1px solid #d8dbe0; font-weight: 700; white-space: nowrap; }
.file-picker-name { padding: 0 14px; color: #6c757d; font-size: 14px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.file-picker-name.has-file { color: #1f2d3d; font-weight: 600; }
.action-row { display: flex; justify-content: flex-end; gap: 10px; align-items: center; }
.btn-analyze { border: 0; background: #321fdb; color: white; border-radius: 6px; padding: 9px 22px; font-weight: 700; }
.btn-analyze:disabled { opacity: .65; cursor: not-allowed; }
.message-box { padding: 11px 14px; margin-bottom: 14px; border-radius: 6px; font-size: 13px; }
.message-box.error { color: #842029; background: #f8d7da; border: 1px solid #f5c2c7; }
.result-wrapper { overflow-x: auto; border: 1px solid #d8dbe0; border-radius: 7px; }
.result-table { width: 100%; min-width: 1050px; border-collapse: collapse; margin: 0; background: white; font-size: 13px; }
.result-table th { padding: 10px 8px; text-align: center; color: white; background: #5b6065; white-space: nowrap; font-weight: 700; }
.result-table td { padding: 10px 8px; border-bottom: 1px solid #e5e7eb; text-align: center; vertical-align: middle; }
.material-cell { text-align: left !important; font-weight: 700; }
.number-cell { text-align: right !important; font-variant-numeric: tabular-nums; }
.status-badge { display: inline-block; padding: 4px 9px; border-radius: 5px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.status-high { color: #842029; background: #f8d7da; }.status-low { color: #664d03; background: #fff3cd; }.status-normal { color: #0f5132; background: #d1e7dd; }.status-insufficient { color: #495057; background: #e9ecef; }
.section-title { font-weight: 800; font-size: 15px; margin-bottom: 10px; }.result-note { color: #6c757d; font-size: 12px; margin-bottom: 12px; }.loading-box { text-align: center; padding: 18px; color: #6c757d; font-size: 14px; }.empty-row { text-align: center !important; color: #6c757d; padding: 24px !important; }
.result-toolbar { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; margin-bottom: 12px; }.result-toolbar-text { flex: 1; }.result-toolbar-search { width: 430px; max-width: 100%; min-width: 0; }.anomaly-filter-row { display: grid; grid-template-columns: minmax(0,1fr) 140px; gap: 10px; }.problem-header { display: flex; align-items: center; justify-content: space-between; gap: 15px; }
@media (max-width:768px){.file-area{flex-direction:column}.action-row{justify-content:stretch}.btn-analyze{width:100%}.result-toolbar{flex-direction:column;align-items:stretch}.result-toolbar-search{width:100%}.anomaly-filter-row{grid-template-columns:1fr}}
</style>
