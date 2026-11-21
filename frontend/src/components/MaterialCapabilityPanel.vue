<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api, queryString } from '../services/api'

const props = withDefaults(defineProps<{ structureType?: string; allowSpecificationEstimate?: boolean }>(), {
  structureType: '', allowSpecificationEstimate: false,
})
interface Capability {
  key: string; name: string; standardUnit: string; status: string; statusLabel: string
  currentModelSupported: boolean; formalAnomalyEnabled: boolean; specificationEstimateAvailable: boolean
  historicalEstimateSupported?: boolean
  patternBasis: string; nameCandidateMaterialCount: number; mappedMaterialCount: number
  historicalProjectCount: number; minimumSampleCount: number
  recognizableSyntheticNameProjectCount: number; missingEngineeringConditions: string[]; reasons: string[]
}
interface Profile {
  key: string; materialKey: string; productName: string; basisLabel: string; outputUnit: string
  lowerRate: number; upperRate: number; rateUnit: string; sourceUrl: string; sourceVersion: string
  conditions: string[]
}
interface Estimate {
  productName: string; workAreaM2: number; basisLabel: string; unit: string
  lowerQuantity: number; upperQuantity: number; rangeKind: string; formalAnomalyEnabled: boolean
  reviewStatus: string; message: string; sourceUrl: string; sourceVersion: string; conditions: string[]
}
const items = ref<Capability[]>([])
const notice = ref('')
const loading = ref(false)
const error = ref('')
const profiles = ref<Profile[]>([])
const profileError = ref('')
const profileKey = ref('')
const area = ref<number | null>(null)
const observed = ref<number | null>(null)
const estimating = ref(false)
const estimateError = ref('')
const result = ref<Estimate | null>(null)
const selectedProfile = computed(() => profiles.value.find(x => x.key === profileKey.value))
let generation = 0

async function loadCapabilities() {
  const current = ++generation
  loading.value = true
  error.value = ''
  try {
    const response = await api<{ notice: string; items: Capability[] }>(
      '/material-capabilities' + queryString({ structureType: props.structureType || undefined }),
    )
    if (current === generation) { items.value = response.items; notice.value = response.notice }
  } catch (err) {
    if (current === generation) { items.value = []; error.value = err instanceof Error ? err.message : '讀取失敗' }
  } finally { if (current === generation) loading.value = false }
}
watch(() => props.structureType, loadCapabilities, { immediate: true })
watch([profileKey, area, observed], () => { result.value = null; estimateError.value = '' })
onMounted(async () => {
  if (!props.allowSpecificationEstimate) return
  try { profiles.value = (await api<{ profiles: Profile[] }>('/material-capabilities/specifications')).profiles }
  catch (err) { profileError.value = err instanceof Error ? err.message : '產品規格讀取失敗' }
})

async function estimate() {
  result.value = null
  estimateError.value = ''
  if (!profileKey.value || !area.value || !Number.isFinite(area.value) || area.value <= 0) {
    estimateError.value = '請選擇指定產品／模板條件，並輸入實際施工面積。'
    return
  }
  const observedQuantity = observed.value == null || String(observed.value).trim() === '' ? null : Number(observed.value)
  if (observedQuantity != null && (!Number.isFinite(observedQuantity) || observedQuantity < 0)) {
    estimateError.value = '實際用量不可小於零。'
    return
  }
  estimating.value = true
  try {
    result.value = await api<Estimate>('/material-capabilities/specification-estimate', {
      method: 'POST', body: JSON.stringify({ profileKey: profileKey.value, workAreaM2: area.value, observedQuantity }),
    })
  } catch (err) { estimateError.value = err instanceof Error ? err.message : '試算失敗' }
  finally { estimating.value = false }
}
function number(value: number) { return value.toLocaleString('zh-TW', { maximumFractionDigits: 4 }) }
</script>

<template>
  <section class="card material-capabilities">
    <div class="card-header">
      <div>
        <h3>材料工項與可估條件</h3>
        <p class="text-muted small mb-0">{{ structureType ? `${structureType} 可比歷史` : '全部結構歷史概況；選定結構後會重新檢查' }}。有穩定規律仍需確認施工面積、規格與工法。</p>
      </div>
      <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="loading" @click="loadCapabilities">重新檢查</button>
    </div>
    <div class="capability-body">
      <div v-if="loading" class="text-muted">正在檢查材料對應與歷史建案...</div>
      <div v-else-if="error" class="notice info">材料工項檢查目前無法載入：{{ error }}。請確認後端已更新；既有估算與保存結果仍可使用。</div>
      <template v-else>
        <p class="text-muted small">{{ notice }}</p>
        <div class="table-wrap">
          <table>
            <thead><tr><th>工項</th><th>目前狀態</th><th>材料候選／正式對應</th><th>可比歷史建案</th><th>規律與未開放原因</th></tr></thead>
            <tbody>
              <tr v-for="item in items" :key="item.key" :data-material-key="item.key">
                <td><strong>{{ item.name }}</strong><div class="text-muted small">{{ item.standardUnit }}</div></td>
                <td><span class="capability-state" :class="{ available: item.formalAnomalyEnabled || item.historicalEstimateSupported }">{{ item.statusLabel }}</span></td>
                <td>{{ item.nameCandidateMaterialCount }}／{{ item.mappedMaterialCount }}</td>
                <td>{{ item.historicalProjectCount }} 件<div class="text-muted small">最低 {{ item.minimumSampleCount }} 件；不按交易列計數</div><div v-if="item.historicalEstimateSupported" class="text-muted small">此欄為交易表候選；新概算會另查已核准決標來源</div><div v-if="item.recognizableSyntheticNameProjectCount" class="text-muted small">其中 {{ item.recognizableSyntheticNameProjectCount }} 件名稱含合成標記</div></td>
                <td class="capability-reasons">
                  <div>{{ item.patternBasis }}</div>
                  <details v-if="item.reasons.length"><summary>查看資料不足或使用限制</summary><ul><li v-for="reason in item.reasons" :key="reason">{{ reason }}</li></ul></details>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <section v-if="allowSpecificationEstimate" class="specification-estimate">
        <h4>依指定產品規格試算用量</h4>
        <p class="text-muted small">適合已知施工面積的養護劑、脫模劑。施工面積不會自動帶入樓地板面積；產品理論用量不是歷史 P15／P50／P85，也不產生正式異常紅燈。</p>
        <div v-if="profileError" class="notice info">產品規格無法載入：{{ profileError }}</div>
        <form v-else @submit.prevent="estimate">
          <div class="specification-fields">
            <label class="field">
              <span>指定產品與施工條件</span>
              <select v-model="profileKey" aria-label="指定產品與施工條件">
                <option value="">請選擇</option>
                <option v-for="profile in profiles" :key="profile.key" :value="profile.key">{{ profile.productName }}</option>
              </select>
            </label>
            <label class="field"><span>{{ selectedProfile?.basisLabel || '實際施工面積' }}（m²）</span><input v-model.number="area" aria-label="實際施工面積" type="number" step="any" min="0" required placeholder="請依圖說或實測輸入" /></label>
            <label class="field"><span>實際用量（選填，{{ selectedProfile?.outputUnit || '先選產品' }}）</span><input v-model.number="observed" aria-label="實際用量" type="number" step="any" min="0" /></label>
          </div>
          <div v-if="selectedProfile" class="specification-source">
            產品理論參數：{{ selectedProfile.lowerRate }}–{{ selectedProfile.upperRate }} {{ selectedProfile.rateUnit }}。
            <a :href="selectedProfile.sourceUrl" target="_blank" rel="noopener noreferrer">原廠產品資料</a>（{{ selectedProfile.sourceVersion }}）
          </div>
          <button type="submit" class="btn btn-primary mt-3" :disabled="estimating || !profiles.length">{{ estimating ? '試算中...' : '依規格試算' }}</button>
        </form>
        <div v-if="estimateError" class="notice error mt-3">{{ estimateError }}</div>
        <div v-if="result" class="specification-result" data-testid="specification-result">
          <strong>{{ result.productName }}：{{ number(result.lowerQuantity) }}–{{ number(result.upperQuantity) }} {{ result.unit }}</strong>
          <p>{{ result.message }}</p>
          <ul><li v-for="condition in result.conditions" :key="condition">{{ condition }}</li></ul>
          <p class="text-muted small mb-0">此試算不建立案件或分析紀錄，不會寫入歷史庫。</p>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.capability-body { padding: 20px; }
.capability-body table { width: 100%; min-width: 740px; }
.capability-body th, .capability-body td { padding: 12px; text-align: left; vertical-align: top; border-bottom: 1px solid #e5eaf0; }
.capability-reasons { max-width: 430px; line-height: 1.6; }
.capability-reasons details { margin-top: 7px; color: #526274; }
.capability-reasons summary { cursor: pointer; }
.capability-reasons ul { padding-left: 20px; margin-bottom: 0; }
.capability-state { display: inline-block; background: #edf2f7; color: #46566a; border-radius: 8px; padding: 5px 9px; font-size: 13px; }
.capability-state.available { background: #e7f0fc; color: #24558c; }
.specification-estimate { border-top: 1px solid #e5eaf0; margin-top: 24px; padding-top: 22px; }
.specification-fields { display: grid; grid-template-columns: 1.5fr 1fr 1fr; gap: 16px; }
.specification-source { margin-top: 12px; color: #526274; font-size: 13px; }
.specification-result { padding: 18px; margin-top: 18px; background: #f0f5fb; border: 1px solid #d7e3f3; border-radius: 10px; }
.specification-result p { margin: 10px 0; }
.specification-result ul { padding-left: 20px; }
@media (max-width: 800px) { .specification-fields { grid-template-columns: 1fr; } .capability-body { padding: 14px; } }
</style>
