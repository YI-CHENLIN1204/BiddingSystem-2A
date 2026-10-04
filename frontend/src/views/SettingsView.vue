<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import DataTable from '../components/DataTable.vue'
import { api, apiForm, queryString } from '../services/api'
import type { CoreMaterial, JsonRecord } from '../types'

type Settings = Record<string, number | string | null>
type Tab = 'model' | 'core' | 'materials' | 'units'

const tab = ref<Tab>('model')
const settings = ref<Settings>({})
const coreMaterials = ref<CoreMaterial[]>([])
const materials = ref<JsonRecord[]>([])
const categories = ref<JsonRecord[]>([])
const units = ref<JsonRecord[]>([])
const unitCategories = ref<JsonRecord[]>([])
const error = ref('')
const message = ref('')
const loading = ref(false)
const materialSearch = ref('')

const settingsFields = [
  ['topK', '相似專案數量 Top K', '1'],
  ['minimumSampleCount', '最低樣本數', '1'],
  ['lowerQuantile', '下界分位數（0–1）', '0.01'],
  ['medianQuantile', '中位分位數（0–1）', '0.01'],
  ['upperQuantile', '上界分位數（0–1）', '0.01'],
  ['similarityExponent', '相似度指數', '0.01'],
  ['timeHalfLifeYears', '時間半衰期（年）', '0.1'],
  ['minimumSimilarityScore', '最低相似度', '0.1'],
  ['structureWeight', '結構權重', '0.01'],
  ['areaWeight', '面積權重', '0.01'],
  ['floorWeight', '樓層權重', '0.01'],
  ['yearWeight', '年份權重', '0.01'],
  ['cityWeight', '縣市權重', '0.01'],
  ['areaDifferenceCutoffRatio', '面積差距截止比例', '0.01'],
  ['groundFloorPenaltyRatio', '地上樓層懲罰比例', '0.01'],
  ['basementFloorPenaltyRatio', '地下樓層懲罰比例', '0.01'],
  ['yearDifferenceCutoffYears', '年份差距截止值', '1'],
] as const

const categoryOptions = computed(() => categories.value.map((item) => ({ id: Number(item.categoryId), name: String(item.categoryName) })))
const unitOptions = computed(() => units.value.map((item) => ({ id: Number(item.unitId), name: String(item.unitName) })))
const unitCategoryOptions = computed(() => unitCategories.value.map((item) => ({ id: Number(item.unitCategoryId), name: String(item.categoryName) })))

const newCategory = ref('')
const newMaterial = ref({ materialName: '', categoryId: 0, unitId: 0 })
const newUnitCategory = ref({ categoryName: '', allowConversion: false, baseUnitName: '' })
const newUnit = ref({ unitName: '', unitCategoryId: 0, toBaseFactor: null as number | null })

function asRows(value: unknown): JsonRecord[] {
  if (Array.isArray(value)) return value as JsonRecord[]
  if (value && typeof value === 'object') {
    const record = value as JsonRecord
    const candidate = record.items ?? record.data
    if (Array.isArray(candidate)) return candidate as JsonRecord[]
  }
  return []
}

async function loadSettings() {
  const response = await api<{ settings: Settings; coreMaterials: CoreMaterial[] }>('/settings')
  settings.value = response.settings
  coreMaterials.value = response.coreMaterials
}

async function loadReferenceData() {
  const [materialData, categoryData, unitData, unitCategoryData] = await Promise.all([
    api<unknown>(`/standard-material${queryString({ search: materialSearch.value })}`),
    api<unknown>('/material-category'),
    api<unknown>('/unit-master'),
    api<unknown>('/unit-category'),
  ])
  materials.value = asRows(materialData)
  categories.value = asRows(categoryData)
  units.value = asRows(unitData)
  unitCategories.value = asRows(unitCategoryData)
}

async function run(action: () => Promise<unknown>, success: string) {
  loading.value = true
  error.value = ''
  message.value = ''
  try {
    await action()
    message.value = success
    await Promise.all([loadSettings(), loadReferenceData()])
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '操作失敗'
  } finally {
    loading.value = false
  }
}

function formFrom(values: Record<string, unknown>) {
  const form = new FormData()
  Object.entries(values).forEach(([key, value]) => form.append(key, value == null ? '' : String(value)))
  return form
}

async function saveSettings() {
  const body = Object.fromEntries(settingsFields.map(([key]) => [key, Number(settings.value[key])]))
  await run(() => api('/settings', { method: 'PUT', body: JSON.stringify(body) }), '模型參數已更新。')
}

async function toggleCore(item: CoreMaterial) {
  await run(() => api('/settings/core-material', {
    method: 'PUT',
    body: JSON.stringify({ materialId: item.materialId, projectType: item.projectType, structureType: item.structureType, isCoreEstimation: !item.isCoreEstimation }),
  }), '核心材料狀態已更新。')
}

async function createCategory() {
  await run(() => apiForm('/material-category/create', formFrom({ categoryName: newCategory.value })), '材料分類已新增。')
  newCategory.value = ''
}
async function createMaterial() {
  await run(() => apiForm('/standard-material/create', formFrom(newMaterial.value)), '標準材料已新增。')
  newMaterial.value.materialName = ''
}
async function createUnitCategory() {
  await run(() => apiForm('/unit-category/create', formFrom(newUnitCategory.value)), '單位類別已新增。')
  newUnitCategory.value.categoryName = ''
  newUnitCategory.value.baseUnitName = ''
}
async function createUnit() {
  await run(() => apiForm('/unit-master/create', formFrom(newUnit.value)), '標準單位已新增。')
  newUnit.value.unitName = ''
}

onMounted(async () => {
  try { await Promise.all([loadSettings(), loadReferenceData()]) }
  catch (caught) { error.value = caught instanceof Error ? caught.message : '無法載入設定' }
})
</script>

<template>
  <div class="page-stack">
    <div class="page-intro"><div><h2>參數與主檔管理</h2><p>集中管理估價模型、核心材料、材料分類與單位主檔。</p></div></div>
    <div v-if="error" class="notice error">{{ error }}</div>
    <div v-if="message" class="notice success">{{ message }}</div>
    <div class="tabs">
      <button v-for="item in ([['model','模型參數'],['core','核心材料'],['materials','材料主檔'],['units','單位主檔']] as const)" :key="item[0]" class="tab" :class="{ active: tab === item[0] }" @click="tab = item[0]">{{ item[1] }}</button>
    </div>

    <form v-if="tab === 'model'" class="card" @submit.prevent="saveSettings">
      <div class="card-header"><h3>估價與異常偵測參數</h3></div>
      <div class="card-body">
        <div class="form-grid cols-3">
          <label v-for="field in settingsFields" :key="field[0]" class="field"><span>{{ field[1] }}</span><input v-model.number="settings[field[0]]" type="number" :step="field[2]" required /></label>
        </div>
        <div class="actions"><button class="button primary" :disabled="loading">儲存參數</button></div>
      </div>
    </form>

    <section v-else-if="tab === 'core'" class="card">
      <div class="card-header"><h3>核心材料情境</h3></div>
      <div class="table-wrap"><table><thead><tr><th>材料</th><th>分類</th><th>建案類型</th><th>結構</th><th>有效專案</th><th>狀態</th></tr></thead><tbody>
        <tr v-for="item in coreMaterials" :key="`${item.materialId}-${item.projectType}-${item.structureType}`"><td>{{ item.materialName }}</td><td>{{ item.category }}</td><td>{{ item.projectType || '通用' }}</td><td>{{ item.structureType || '通用' }}</td><td>{{ item.validProjectCount ?? 0 }}</td><td><button class="button small" :class="item.isCoreEstimation ? 'primary' : 'secondary'" @click="toggleCore(item)">{{ item.isCoreEstimation ? '已啟用' : '已停用' }}</button></td></tr>
      </tbody></table></div>
    </section>

    <template v-else-if="tab === 'materials'">
      <div class="two-column">
        <form class="card" @submit.prevent="createMaterial"><div class="card-header"><h3>新增標準材料</h3></div><div class="card-body form-grid"><label class="field span-2"><span>材料名稱</span><input v-model="newMaterial.materialName" required /></label><label class="field"><span>材料分類</span><select v-model.number="newMaterial.categoryId" required><option :value="0" disabled>請選擇</option><option v-for="item in categoryOptions" :key="item.id" :value="item.id">{{ item.name }}</option></select></label><label class="field"><span>標準單位</span><select v-model.number="newMaterial.unitId" required><option :value="0" disabled>請選擇</option><option v-for="item in unitOptions" :key="item.id" :value="item.id">{{ item.name }}</option></select></label><div class="actions span-2"><button class="button primary">新增材料</button></div></div></form>
        <form class="card" @submit.prevent="createCategory"><div class="card-header"><h3>新增材料分類</h3></div><div class="card-body"><label class="field"><span>分類名稱</span><input v-model="newCategory" required /></label><div class="actions"><button class="button primary">新增分類</button></div></div></form>
      </div>
      <section class="card"><div class="card-body toolbar"><label class="field"><span>搜尋材料</span><input v-model="materialSearch" @keyup.enter="loadReferenceData" /></label><button class="button secondary" @click="loadReferenceData">查詢</button></div><DataTable :rows="materials" /></section>
    </template>

    <template v-else>
      <div class="two-column">
        <form class="card" @submit.prevent="createUnit"><div class="card-header"><h3>新增標準單位</h3></div><div class="card-body form-grid"><label class="field"><span>單位名稱</span><input v-model="newUnit.unitName" required /></label><label class="field"><span>單位類別</span><select v-model.number="newUnit.unitCategoryId" required><option :value="0" disabled>請選擇</option><option v-for="item in unitCategoryOptions" :key="item.id" :value="item.id">{{ item.name }}</option></select></label><label class="field span-2"><span>換算為基準單位倍率</span><input v-model.number="newUnit.toBaseFactor" type="number" step="0.0001" /></label><div class="actions span-2"><button class="button primary">新增單位</button></div></div></form>
        <form class="card" @submit.prevent="createUnitCategory"><div class="card-header"><h3>新增單位類別</h3></div><div class="card-body"><label class="field"><span>類別名稱</span><input v-model="newUnitCategory.categoryName" required /></label><label style="display:block;margin:14px 0"><input v-model="newUnitCategory.allowConversion" type="checkbox" /> 允許單位換算</label><label v-if="newUnitCategory.allowConversion" class="field"><span>基準單位名稱</span><input v-model="newUnitCategory.baseUnitName" required /></label><div class="actions"><button class="button primary">新增類別</button></div></div></form>
      </div>
      <section class="card"><div class="card-header"><h3>單位清單</h3></div><DataTable :rows="units" /></section>
    </template>
  </div>
</template>
