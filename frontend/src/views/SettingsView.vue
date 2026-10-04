<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, apiForm, queryString } from '../services/api'
import type { CoreMaterial } from '../types'

type Tab = 'model' | 'core' | 'masters'
type MasterTab = 'categories' | 'materials' | 'units'
interface SettingsRecord { [key: string]: number | string | null }
interface Category { categoryId: number; categoryName: string; isActive: boolean; sortOrder: number; materialCount: number }
interface Material { materialId: number; materialName: string; categoryId: number; categoryName: string; unitId: number; unitName: string; isActive: boolean; dictionaryCount: number; historicalCount: number; finalBidItemCount: number }
interface UnitCategory { unitCategoryId: number; categoryName: string; allowConversion: boolean; baseUnitName?: string | null }
interface Unit { unitId: number; unitName: string; unitCategoryId: number; categoryName: string; allowConversion: boolean; toBaseFactor?: number | null; isBaseUnit: boolean; isActive: boolean }

const tab = ref<Tab>('model')
const masterTab = ref<MasterTab>('categories')
const settings = ref<SettingsRecord>({})
const form = ref<Record<string, number>>({})
const updatedAt = ref('')
const updatedBy = ref('')
const coreMaterials = ref<CoreMaterial[]>([])
const categories = ref<Category[]>([])
const materials = ref<Material[]>([])
const units = ref<Unit[]>([])
const unitCategories = ref<UnitCategory[]>([])
const loading = ref(false)
const error = ref('')
const message = ref('')

const coreFilter = ref({ projectType: '', structureType: '', category: '', dataStatus: 'available', search: '' })
const materialFilter = ref({ search: '', categoryId: '', unitId: '', active: '' })
const unitFilter = ref({ search: '', unitCategoryId: '', active: '' })
const newCategory = ref('')
const newMaterial = ref({ materialName: '', categoryId: 0, unitId: 0 })
const newUnitCategory = ref({ categoryName: '', allowConversion: false, baseUnitName: '' })
const newUnit = ref({ unitName: '', unitCategoryId: 0, toBaseFactor: null as number | null })
const editingCategory = ref<Category | null>(null)
const editingMaterial = ref<Material | null>(null)
const editingUnit = ref<Unit | null>(null)

const activeCategories = computed(() => categories.value.filter(x => x.isActive))
const activeUnits = computed(() => units.value.filter(x => x.isActive))
const projectTypes = computed(() => [...new Set(coreMaterials.value.map(x => x.projectType).filter(Boolean))].sort())
const structureTypes = computed(() => [...new Set(coreMaterials.value.map(x => x.structureType).filter(Boolean))].sort())
const coreCategories = computed(() => [...new Set(coreMaterials.value.map(x => x.category).filter(Boolean))].sort())
const weightTotal = computed(() => ['structureWeight', 'areaWeight', 'floorWeight', 'cityWeight', 'yearWeight'].reduce((sum, key) => sum + Number(form.value[key] ?? 0), 0))

const filteredCore = computed(() => coreMaterials.value.filter(item => {
  if (coreFilter.value.projectType && item.projectType !== coreFilter.value.projectType) return false
  if (coreFilter.value.structureType && item.structureType !== coreFilter.value.structureType) return false
  if (coreFilter.value.category && item.category !== coreFilter.value.category) return false
  if (coreFilter.value.dataStatus === 'available' && !(item.validProjectCount ?? 0)) return false
  if (coreFilter.value.dataStatus === 'empty' && (item.validProjectCount ?? 0) > 0) return false
  return !coreFilter.value.search || item.materialName.toLowerCase().includes(coreFilter.value.search.toLowerCase())
}).sort((a, b) => (b.validProjectCount ?? 0) - (a.validProjectCount ?? 0)))

const filteredMaterials = computed(() => materials.value.filter(item => {
  if (materialFilter.value.search && !item.materialName.toLowerCase().includes(materialFilter.value.search.toLowerCase())) return false
  if (materialFilter.value.categoryId && item.categoryId !== Number(materialFilter.value.categoryId)) return false
  if (materialFilter.value.unitId && item.unitId !== Number(materialFilter.value.unitId)) return false
  if (materialFilter.value.active !== '' && item.isActive !== (materialFilter.value.active === 'true')) return false
  return true
}))

const filteredUnits = computed(() => units.value.filter(item => {
  if (unitFilter.value.search && !item.unitName.toLowerCase().includes(unitFilter.value.search.toLowerCase())) return false
  if (unitFilter.value.unitCategoryId && item.unitCategoryId !== Number(unitFilter.value.unitCategoryId)) return false
  if (unitFilter.value.active !== '' && item.isActive !== (unitFilter.value.active === 'true')) return false
  return true
}))

function formData(values: Record<string, unknown>) {
  const data = new FormData()
  Object.entries(values).forEach(([key, value]) => data.append(key, value == null ? '' : String(value)))
  return data
}

function formatDate(value: string) {
  return value ? new Date(value).toLocaleString('zh-TW') : '尚無紀錄'
}

async function loadSettings() {
  const response = await api<{ settings: SettingsRecord; coreMaterials: CoreMaterial[] }>('/settings')
  settings.value = response.settings
  coreMaterials.value = response.coreMaterials
  const s = response.settings
  form.value = {
    topK: Number(s.topK), minimumSampleCount: Number(s.minimumSampleCount),
    lowerQuantilePercent: Number(s.lowerQuantile) * 100, medianQuantilePercent: Number(s.medianQuantile) * 100, upperQuantilePercent: Number(s.upperQuantile) * 100,
    similarityExponent: Number(s.similarityExponent), timeHalfLifeYears: Number(s.timeHalfLifeYears), minimumSimilarityScore: Number(s.minimumSimilarityScore),
    structureWeight: Number(s.structureWeight), areaWeight: Number(s.areaWeight), floorWeight: Number(s.floorWeight), yearWeight: Number(s.yearWeight), cityWeight: Number(s.cityWeight),
    areaDifferenceCutoffPercent: Number(s.areaDifferenceCutoffRatio) * 100,
    groundFloorPenaltyPercent: Number(s.groundFloorPenaltyRatio) * 100,
    basementFloorPenaltyPercent: Number(s.basementFloorPenaltyRatio) * 100,
    yearDifferenceCutoffYears: Number(s.yearDifferenceCutoffYears),
  }
  updatedAt.value = String(s.updatedAt ?? '')
  updatedBy.value = String(s.updatedBy ?? '')
}

async function loadMasters() {
  const [categoryData, materialData, unitData, unitCategoryData] = await Promise.all([
    api<Category[]>('/material-category'), api<Material[]>('/standard-material'),
    api<Unit[]>('/unit-master'), api<UnitCategory[]>('/unit-category'),
  ])
  categories.value = categoryData
  materials.value = materialData
  units.value = unitData
  unitCategories.value = unitCategoryData
}

async function run(action: () => Promise<unknown>, success: string, reload: 'all' | 'settings' | 'masters' = 'all') {
  loading.value = true; error.value = ''; message.value = ''
  try {
    await action()
    if (reload === 'all' || reload === 'settings') await loadSettings()
    if (reload === 'all' || reload === 'masters') await loadMasters()
    message.value = success
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '操作失敗'
  } finally { loading.value = false }
}

async function saveSettings() {
  const f = form.value
  await run(() => api('/settings', { method: 'PUT', body: JSON.stringify({
    topK: f.topK, minimumSampleCount: f.minimumSampleCount,
    lowerQuantile: f.lowerQuantilePercent / 100, medianQuantile: f.medianQuantilePercent / 100, upperQuantile: f.upperQuantilePercent / 100,
    similarityExponent: f.similarityExponent, timeHalfLifeYears: f.timeHalfLifeYears, minimumSimilarityScore: f.minimumSimilarityScore,
    structureWeight: f.structureWeight, areaWeight: f.areaWeight, floorWeight: f.floorWeight, yearWeight: f.yearWeight, cityWeight: f.cityWeight,
    areaDifferenceCutoffRatio: f.areaDifferenceCutoffPercent / 100,
    groundFloorPenaltyRatio: f.groundFloorPenaltyPercent / 100,
    basementFloorPenaltyRatio: f.basementFloorPenaltyPercent / 100,
    yearDifferenceCutoffYears: f.yearDifferenceCutoffYears,
  }) }), '模型參數已更新。', 'settings')
}

async function toggleCore(item: CoreMaterial) {
  await run(() => api('/settings/core-material', { method: 'PUT', body: JSON.stringify({
    materialId: item.materialId, projectType: item.projectType, structureType: item.structureType, isCoreEstimation: !item.isCoreEstimation,
  }) }), '核心材料狀態已更新。', 'settings')
}

async function createCategory() { await run(() => apiForm('/material-category/create', formData({ categoryName: newCategory.value })), '材料分類已新增。', 'masters'); newCategory.value = '' }
async function saveCategory() { if (!editingCategory.value) return; await run(() => apiForm('/material-category/update', formData(editingCategory.value)), '材料分類已更新。', 'masters'); editingCategory.value = null }
async function toggleCategory(item: Category) { await run(() => apiForm('/material-category/set-active', formData({ categoryId: item.categoryId, isActive: !item.isActive })), '材料分類狀態已更新。', 'masters') }

async function createMaterial() { await run(() => apiForm('/standard-material/create', formData(newMaterial.value)), '標準材料已新增。', 'masters'); newMaterial.value.materialName = '' }
async function saveMaterial() { if (!editingMaterial.value) return; await run(() => apiForm('/standard-material/update', formData(editingMaterial.value)), '標準材料已更新。', 'masters'); editingMaterial.value = null }
async function toggleMaterial(item: Material) { await run(() => apiForm('/standard-material/set-active', formData({ materialId: item.materialId, isActive: !item.isActive })), '標準材料狀態已更新。', 'masters') }

async function createUnitCategory() { await run(() => apiForm('/unit-category/create', formData(newUnitCategory.value)), '單位類別已新增。', 'masters'); newUnitCategory.value = { categoryName: '', allowConversion: false, baseUnitName: '' } }
async function createUnit() { await run(() => apiForm('/unit-master/create', formData(newUnit.value)), '標準單位已新增。', 'masters'); newUnit.value.unitName = '' }
async function saveUnit() { if (!editingUnit.value) return; await run(() => apiForm('/unit-master/update', formData(editingUnit.value)), '標準單位已更新。', 'masters'); editingUnit.value = null }
async function toggleUnit(item: Unit) { await run(() => apiForm('/unit-master/set-active', formData({ unitId: item.unitId, isActive: !item.isActive })), '標準單位狀態已更新。', 'masters') }

onMounted(async () => {
  try { await Promise.all([loadSettings(), loadMasters()]) }
  catch (caught) { error.value = caught instanceof Error ? caught.message : '無法載入參數資料' }
})
</script>

<template>
  <div class="settings-page mt-4">
    <h2 class="page-title">參數管理</h2>
    <div class="page-description">管理估算與異常模型、核心材料及材料主檔設定。</div>
    <div v-if="error" class="alert alert-danger"><strong>注意：</strong> {{ error }}</div>
    <div v-if="message" class="alert alert-success alert-dismissible"><strong>成功：</strong> {{ message }}<button type="button" class="btn-close" @click="message = ''"></button></div>

    <div class="settings-card">
      <ul class="nav nav-tabs">
        <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'model' }" @click="tab = 'model'">估算與異常模型</button></li>
        <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'core' }" @click="tab = 'core'">核心材料設定</button></li>
        <li class="nav-item"><button class="nav-link" :class="{ active: tab === 'masters' }" @click="tab = 'masters'">材料主檔管理</button></li>
      </ul>

      <div class="tab-content-area">
        <form v-if="tab === 'model'" @submit.prevent="saveSettings">
          <section class="model-section"><header><div class="section-title">樣本與合理區間</div><div class="section-description">設定每項材料採用的歷史案件數，以及 P15 / P50 / P85 合理用量區間。</div></header><div class="section-body"><div class="parameter-grid cols-2"><label><span>每項材料參考上限（Top K）</span><input v-model.number="form.topK" type="number" min="1" required /><small>每項材料最多採用多少個相似歷史建案。</small></label><label><span>最低有效樣本數</span><input v-model.number="form.minimumSampleCount" type="number" min="1" required /><small>低於此數量時，不進行合理區間與異常判定。</small></label></div><div class="parameter-grid cols-3 mt-3"><label v-for="entry in [['lowerQuantilePercent','合理區間下界（P15）'],['medianQuantilePercent','參考中位數（P50）'],['upperQuantilePercent','合理區間上界（P85）']]" :key="entry[0]"><span>{{ entry[1] }}</span><div class="input-group"><input v-model.number="form[entry[0]]" type="number" min="0" max="100" step="0.1" required /><span class="input-group-text">%</span></div></label></div></div></section>
          <section class="model-section"><header class="between"><div><div class="section-title">相似建案權重</div><div class="section-description">控制結構、面積、樓層、縣市與年份對建案相似度的影響。</div></div><div class="weight-total">權重合計 <strong>{{ weightTotal }}</strong></div></header><div class="section-body"><div class="parameter-grid cols-5"><label v-for="entry in [['structureWeight','結構權重'],['areaWeight','面積權重'],['floorWeight','樓層權重'],['cityWeight','縣市權重'],['yearWeight','年份權重']]" :key="entry[0]"><span>{{ entry[1] }}</span><input v-model.number="form[entry[0]]" type="number" min="0" step="0.1" required /></label></div></div></section>
          <section class="model-section"><header><div class="section-title">相似度與時間控制</div><div class="section-description">設定相似案件門檻、高相似案件的加權程度與歷史資料的時間衰減。</div></header><div class="section-body"><div class="parameter-grid cols-4"><label><span>相似度強化指數</span><input v-model.number="form.similarityExponent" type="number" min="0.1" step="0.1" required /></label><label><span>最低相似度</span><div class="input-group"><input v-model.number="form.minimumSimilarityScore" type="number" min="0" max="100" step="0.1" required /><span class="input-group-text">分</span></div></label><label><span>時間半衰期</span><div class="input-group"><input v-model.number="form.timeHalfLifeYears" type="number" min="0.1" step="0.1" required /><span class="input-group-text">年</span></div></label><label><span>年份差距截止</span><div class="input-group"><input v-model.number="form.yearDifferenceCutoffYears" type="number" min="1" required /><span class="input-group-text">年</span></div></label></div></div></section>
          <section class="model-section"><header><div class="section-title">差距懲罰</div><div class="section-description">控制面積與樓層差異造成的相似度扣分程度。</div></header><div class="section-body"><div class="parameter-grid cols-3"><label v-for="entry in [['areaDifferenceCutoffPercent','面積差距截止比例'],['groundFloorPenaltyPercent','地上樓層懲罰比例'],['basementFloorPenaltyPercent','地下樓層懲罰比例']]" :key="entry[0]"><span>{{ entry[1] }}</span><div class="input-group"><input v-model.number="form[entry[0]]" type="number" min="0" max="100" step="0.1" required /><span class="input-group-text">%</span></div></label></div></div></section>
          <div class="save-bar"><div class="text-muted small">最後更新：{{ formatDate(updatedAt) }}<span v-if="updatedBy"> ／ {{ updatedBy }}</span></div><button class="btn-save" type="submit" :disabled="loading">{{ loading ? '儲存中...' : '儲存模型參數' }}</button></div>
        </form>

        <div v-else-if="tab === 'core'">
          <div class="mb-3"><div class="fw-bold">核心材料快速估算設定</div><div class="text-muted small">依建案類型與結構篩選材料；有效建案數才是模型可使用的樣本數。</div></div>
          <section class="section-card"><header><div class="section-title">材料篩選</div><div class="section-description">建議先選擇建案類型與結構，再依有效歷史建案數挑選核心材料。</div></header><div class="section-body"><div class="row g-3"><div class="col-lg-3 col-md-6"><label class="form-label fw-bold">建案類型</label><select v-model="coreFilter.projectType" class="form-select"><option value="">全部</option><option v-for="entry in projectTypes" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-lg-3 col-md-6"><label class="form-label fw-bold">結構類型</label><select v-model="coreFilter.structureType" class="form-select"><option value="">全部</option><option v-for="entry in structureTypes" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-lg-3 col-md-6"><label class="form-label fw-bold">材料類別</label><select v-model="coreFilter.category" class="form-select"><option value="">全部</option><option v-for="entry in coreCategories" :key="entry" :value="entry">{{ entry }}</option></select></div><div class="col-lg-3 col-md-6"><label class="form-label fw-bold">資料狀態</label><select v-model="coreFilter.dataStatus" class="form-select"><option value="available">有歷史資料</option><option value="">全部</option><option value="empty">尚無歷史資料</option></select></div><div class="col-12"><label class="form-label fw-bold">搜尋材料</label><input v-model="coreFilter.search" class="form-control" placeholder="輸入材料名稱，例如：SD420、混凝土、模板..." /></div></div></div></section>
          <div class="d-flex justify-content-between mb-2 text-muted small"><span>目前顯示 <strong>{{ filteredCore.length }}</strong> 筆材料情境</span><span>※ 同一材料在不同「建案類型 × 結構」會分開統計</span></div>
          <div class="admin-table-wrapper"><table class="admin-table"><thead><tr><th>材料</th><th>類別</th><th>建案類型</th><th>結構</th><th>有效建案數</th><th>歷史筆數</th><th>核心估算</th></tr></thead><tbody><tr v-for="item in filteredCore" :key="`${item.materialId}-${item.projectType}-${item.structureType}`"><td>{{ item.materialName }} <span class="text-muted">({{ item.unitName }})</span></td><td>{{ item.category || '未分類' }}</td><td><span class="badge bg-secondary">{{ item.projectType || '通用' }}</span></td><td><span class="badge bg-secondary">{{ item.structureType || '通用' }}</span></td><td><strong>{{ item.validProjectCount ?? 0 }}</strong> 案</td><td>{{ item.historyRowCount ?? 0 }} 筆</td><td><div class="form-check form-switch d-inline-block"><input class="form-check-input" type="checkbox" :checked="item.isCoreEstimation" @change="toggleCore(item)" /></div></td></tr><tr v-if="!filteredCore.length"><td colspan="7" class="text-muted py-4">目前沒有符合條件的材料資料</td></tr></tbody></table></div>
        </div>

        <div v-else>
          <ul class="nav nav-pills mb-3"><li class="nav-item" v-for="entry in ([['categories','材料分類'],['materials','標準材料'],['units','標準單位']] as const)" :key="entry[0]"><button class="nav-link" :class="{ active: masterTab === entry[0] }" @click="masterTab = entry[0]">{{ entry[1] }}</button></li></ul>
          <template v-if="masterTab === 'categories'"><form class="section-card" @submit.prevent="createCategory"><header><div class="section-title">新增材料分類</div></header><div class="section-body d-flex gap-2"><input v-model="newCategory" class="form-control" placeholder="分類名稱" required /><button class="btn btn-primary text-nowrap" :disabled="loading">新增分類</button></div></form><div class="admin-table-wrapper"><table class="admin-table"><thead><tr><th>排序</th><th>分類名稱</th><th>材料數</th><th>狀態</th><th>操作</th></tr></thead><tbody><tr v-for="item in categories" :key="item.categoryId"><td>{{ item.sortOrder }}</td><td>{{ item.categoryName }}</td><td>{{ item.materialCount }}</td><td><span class="badge" :class="item.isActive ? 'bg-success' : 'bg-secondary'">{{ item.isActive ? '啟用' : '停用' }}</span></td><td><button class="btn btn-sm btn-outline-primary me-2" @click="editingCategory = { ...item }">編輯</button><button class="btn btn-sm" :class="item.isActive ? 'btn-outline-danger' : 'btn-outline-success'" @click="toggleCategory(item)">{{ item.isActive ? '停用' : '啟用' }}</button></td></tr></tbody></table></div></template>
          <template v-else-if="masterTab === 'materials'"><form class="section-card" @submit.prevent="createMaterial"><header><div class="section-title">新增標準材料</div></header><div class="section-body row g-2"><div class="col-md-5"><input v-model="newMaterial.materialName" class="form-control" placeholder="材料名稱" required /></div><div class="col-md-3"><select v-model.number="newMaterial.categoryId" class="form-select" required><option :value="0" disabled>材料分類</option><option v-for="item in activeCategories" :key="item.categoryId" :value="item.categoryId">{{ item.categoryName }}</option></select></div><div class="col-md-3"><select v-model.number="newMaterial.unitId" class="form-select" required><option :value="0" disabled>標準單位</option><option v-for="item in activeUnits" :key="item.unitId" :value="item.unitId">{{ item.unitName }}</option></select></div><div class="col-md-1"><button class="btn btn-primary w-100">新增</button></div></div></form><div class="section-card"><div class="section-body row g-2"><div class="col-md-6"><input v-model="materialFilter.search" class="form-control" placeholder="搜尋材料" /></div><div class="col-md-3"><select v-model="materialFilter.categoryId" class="form-select"><option value="">全部分類</option><option v-for="item in categories" :key="item.categoryId" :value="String(item.categoryId)">{{ item.categoryName }}</option></select></div><div class="col-md-3"><select v-model="materialFilter.active" class="form-select"><option value="">全部狀態</option><option value="true">啟用</option><option value="false">停用</option></select></div></div></div><div class="admin-table-wrapper"><table class="admin-table"><thead><tr><th>材料</th><th>分類</th><th>單位</th><th>字典</th><th>歷史</th><th>狀態</th><th>操作</th></tr></thead><tbody><tr v-for="item in filteredMaterials" :key="item.materialId"><td>{{ item.materialName }}</td><td>{{ item.categoryName }}</td><td>{{ item.unitName }}</td><td>{{ item.dictionaryCount }}</td><td>{{ item.historicalCount }}</td><td><span class="badge" :class="item.isActive ? 'bg-success' : 'bg-secondary'">{{ item.isActive ? '啟用' : '停用' }}</span></td><td><button class="btn btn-sm btn-outline-primary me-2" @click="editingMaterial = { ...item }">編輯</button><button class="btn btn-sm" :class="item.isActive ? 'btn-outline-danger' : 'btn-outline-success'" @click="toggleMaterial(item)">{{ item.isActive ? '停用' : '啟用' }}</button></td></tr></tbody></table></div></template>
          <template v-else><div class="row g-3"><div class="col-lg-7"><form class="section-card" @submit.prevent="createUnit"><header><div class="section-title">新增標準單位</div></header><div class="section-body row g-2"><div class="col-md-4"><input v-model="newUnit.unitName" class="form-control" placeholder="單位名稱" required /></div><div class="col-md-4"><select v-model.number="newUnit.unitCategoryId" class="form-select" required><option :value="0" disabled>單位類別</option><option v-for="item in unitCategories" :key="item.unitCategoryId" :value="item.unitCategoryId">{{ item.categoryName }}</option></select></div><div class="col-md-3"><input v-model.number="newUnit.toBaseFactor" class="form-control" type="number" step="any" placeholder="換算倍率" /></div><div class="col-md-1"><button class="btn btn-primary w-100">新增</button></div></div></form></div><div class="col-lg-5"><form class="section-card" @submit.prevent="createUnitCategory"><header><div class="section-title">新增單位類別</div></header><div class="section-body"><input v-model="newUnitCategory.categoryName" class="form-control mb-2" placeholder="類別名稱" required /><div class="form-check mb-2"><input id="allowConvert" v-model="newUnitCategory.allowConversion" class="form-check-input" type="checkbox" /><label class="form-check-label" for="allowConvert">允許換算</label></div><input v-if="newUnitCategory.allowConversion" v-model="newUnitCategory.baseUnitName" class="form-control mb-2" placeholder="基準單位名稱" required /><button class="btn btn-primary">新增類別</button></div></form></div></div><div class="section-card"><div class="section-body row g-2"><div class="col-md-6"><input v-model="unitFilter.search" class="form-control" placeholder="搜尋單位" /></div><div class="col-md-3"><select v-model="unitFilter.unitCategoryId" class="form-select"><option value="">全部類別</option><option v-for="item in unitCategories" :key="item.unitCategoryId" :value="String(item.unitCategoryId)">{{ item.categoryName }}</option></select></div><div class="col-md-3"><select v-model="unitFilter.active" class="form-select"><option value="">全部狀態</option><option value="true">啟用</option><option value="false">停用</option></select></div></div></div><div class="admin-table-wrapper"><table class="admin-table"><thead><tr><th>單位</th><th>類別</th><th>換算倍率</th><th>基準單位</th><th>狀態</th><th>操作</th></tr></thead><tbody><tr v-for="item in filteredUnits" :key="item.unitId"><td>{{ item.unitName }}</td><td>{{ item.categoryName }}</td><td>{{ item.toBaseFactor ?? '不換算' }}</td><td>{{ item.isBaseUnit ? '是' : '否' }}</td><td><span class="badge" :class="item.isActive ? 'bg-success' : 'bg-secondary'">{{ item.isActive ? '啟用' : '停用' }}</span></td><td><button class="btn btn-sm btn-outline-primary me-2" @click="editingUnit = { ...item }">編輯</button><button class="btn btn-sm" :class="item.isActive ? 'btn-outline-danger' : 'btn-outline-success'" @click="toggleUnit(item)">{{ item.isActive ? '停用' : '啟用' }}</button></td></tr></tbody></table></div></template>
        </div>
      </div>
    </div>

    <div v-if="editingCategory" class="modal-layer" @mousedown.self="editingCategory = null"><div class="modal-dialog"><form class="modal-content" @submit.prevent="saveCategory"><div class="modal-header"><h5 class="modal-title">編輯材料分類</h5><button type="button" class="btn-close" @click="editingCategory = null"></button></div><div class="modal-body"><label class="form-label">分類名稱</label><input v-model="editingCategory.categoryName" class="form-control" required /></div><div class="modal-footer"><button type="button" class="btn btn-secondary" @click="editingCategory = null">取消</button><button class="btn btn-primary">儲存</button></div></form></div></div>
    <div v-if="editingMaterial" class="modal-layer" @mousedown.self="editingMaterial = null"><div class="modal-dialog"><form class="modal-content" @submit.prevent="saveMaterial"><div class="modal-header"><h5 class="modal-title">編輯標準材料</h5><button type="button" class="btn-close" @click="editingMaterial = null"></button></div><div class="modal-body"><label class="form-label">材料名稱</label><input v-model="editingMaterial.materialName" class="form-control mb-3" required /><label class="form-label">材料分類</label><select v-model.number="editingMaterial.categoryId" class="form-select mb-3"><option v-for="item in categories" :key="item.categoryId" :value="item.categoryId">{{ item.categoryName }}</option></select><label class="form-label">標準單位</label><select v-model.number="editingMaterial.unitId" class="form-select"><option v-for="item in units" :key="item.unitId" :value="item.unitId">{{ item.unitName }}</option></select></div><div class="modal-footer"><button type="button" class="btn btn-secondary" @click="editingMaterial = null">取消</button><button class="btn btn-primary">儲存</button></div></form></div></div>
    <div v-if="editingUnit" class="modal-layer" @mousedown.self="editingUnit = null"><div class="modal-dialog"><form class="modal-content" @submit.prevent="saveUnit"><div class="modal-header"><h5 class="modal-title">編輯標準單位</h5><button type="button" class="btn-close" @click="editingUnit = null"></button></div><div class="modal-body"><label class="form-label">單位名稱</label><input v-model="editingUnit.unitName" class="form-control mb-3" required /><label class="form-label">單位類別</label><select v-model.number="editingUnit.unitCategoryId" class="form-select mb-3"><option v-for="item in unitCategories" :key="item.unitCategoryId" :value="item.unitCategoryId">{{ item.categoryName }}</option></select><label class="form-label">換算倍率</label><input v-model.number="editingUnit.toBaseFactor" class="form-control" type="number" step="any" /></div><div class="modal-footer"><button type="button" class="btn btn-secondary" @click="editingUnit = null">取消</button><button class="btn btn-primary">儲存</button></div></form></div></div>
  </div>
</template>

<style scoped>
.settings-page { max-width: 96vw; margin: 0 auto; padding-bottom: 40px; }
.page-title { font-size: 1.55rem; font-weight: 800; margin-bottom: 4px; }
.page-description { color: #6c757d; margin-bottom: 18px; font-size: 14px; }
.settings-card { background: #fff; border: 1px solid #d8dbe0; border-radius: 8px; box-shadow: 0 2px 8px rgba(20,29,35,.06); overflow: hidden; }
.settings-card > .nav-tabs { padding: 0 18px; }
.settings-card > .nav-tabs .nav-link { color: #555; font-weight: 700; border: 0; padding: 15px 18px; }
.settings-card > .nav-tabs .nav-link.active { color: #321fdb; border-bottom: 3px solid #321fdb; background: transparent; }
.tab-content-area { padding: 22px; }
.model-section, .section-card { border: 1px solid #e3e5e8; border-radius: 8px; margin-bottom: 14px; background: #fff; overflow: hidden; }
.model-section header, .section-card header { padding: 11px 16px; background: #f8f9fa; border-bottom: 1px solid #e3e5e8; }
.model-section header.between { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.section-title { color: #321fdb; font-size: 14px; font-weight: 800; margin: 0; }
.section-description { margin-top: 2px; color: #6c757d; font-size: 11px; }
.section-body { padding: 14px 16px; }
.parameter-grid { display: grid; gap: 14px; }.parameter-grid.cols-2 { grid-template-columns: repeat(2,1fr); }.parameter-grid.cols-3 { grid-template-columns: repeat(3,1fr); }.parameter-grid.cols-4 { grid-template-columns: repeat(4,1fr); }.parameter-grid.cols-5 { grid-template-columns: repeat(5,1fr); }
.parameter-grid label > span { display: block; font-size: 13px; font-weight: 700; margin-bottom: 5px; }.parameter-grid input { width: 100%; min-height: 38px; padding: .375rem .75rem; border: 1px solid #dee2e6; border-radius: .375rem; }.parameter-grid small { display: block; color: #6c757d; font-size: 11px; margin-top: 4px; }
.weight-total { background: #ebe8ff; color: #321fdb; border: 1px solid #d9d4ff; border-radius: 6px; padding: 6px 10px; font-size: 12px; white-space: nowrap; }
.save-bar { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 12px 2px 0; }.btn-save { background: #321fdb; color: #fff; border: 0; border-radius: 6px; padding: 9px 22px; font-weight: 800; }
.admin-table-wrapper { overflow-x: auto; }.admin-table { width: 100%; border-collapse: collapse; font-size: 14px; }.admin-table thead th { background: #5b5f63; color: #fff; padding: 11px 14px; text-align: center; white-space: nowrap; }.admin-table tbody td { padding: 11px 14px; border-bottom: 1px solid #e5e7eb; text-align: center; vertical-align: middle; }.admin-table tbody tr:hover { background: #f8f9fa; }
.modal-layer { position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center; padding: 1.5rem; background: rgba(0,0,0,.5); }.modal-layer .modal-dialog { width: min(500px,100%); margin: auto; }
@media (max-width: 1200px) { .parameter-grid.cols-5 { grid-template-columns: repeat(3,1fr); }.parameter-grid.cols-4 { grid-template-columns: repeat(2,1fr); } }
@media (max-width: 760px) { .parameter-grid.cols-2,.parameter-grid.cols-3,.parameter-grid.cols-4,.parameter-grid.cols-5 { grid-template-columns: 1fr; }.model-section header.between,.save-bar { align-items: stretch; flex-direction: column; } }
</style>
