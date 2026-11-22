<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../services/api'

interface Target {
  projectType: string; structureType: string; city: string; totalFloorArea: number | null
  siteArea: number | null; floorCount: number | null; basementFloorCount: number | null; projectDate: string
}
interface Material { materialId: number; materialName: string; unitName: string }
interface Profile { key: string; name: string; unit: string; basis: string; structures: string[]; notice: string; materials: Material[] }
interface Reference {
  projectId: number; projectName: string; structureType: string; projectDate: string
  basisArea: number; quantity: number; usagePerArea: number; sourceRowCount: number
  similarityScore: number; finalWeight: number; finalBidOnlyRowCount: number
}
interface Estimate {
  materialName: string; unit: string; basisLabel: string; hasEnoughSamples: boolean; status: string; message: string
  candidateProjectCount: number; availableProjectCount: number; referenceProjectCount: number; minimumSampleCount: number
  recognizableSyntheticNameProjectCount: number; effectiveSampleSize: number; largestWeightSharePercent: number
  lowerQuantile: number; medianQuantile: number; upperQuantile: number
  lowerQuantity: number | null; medianQuantity: number | null; upperQuantity: number | null
  observedQuantity: number | null; references: Reference[]; excludedProjects: { projectId: number; reason: string }[]
  warnings: string[]; priceLimitation: string
}
const props = defineProps<{ target: Target }>()
const profiles = ref<Profile[]>([])
const profileKey = ref('structural-steel')
const materialId = ref<number | ''>('')
const observed = ref<number | ''>('')
const excludeProjectId = ref<number | ''>('')
const result = ref<Estimate | null>(null)
const loading = ref(false)
const optionsError = ref('')
const error = ref('')
const selected = computed(() => profiles.value.find(x => x.key === profileKey.value))
const structureSupported = computed(() => selected.value?.structures.includes(props.target.structureType) ?? false)
let generation = 0
onMounted(async () => {
  try { profiles.value = (await api<{ profiles: Profile[] }>('/bulk-material-estimates/options')).profiles }
  catch (err) { optionsError.value = err instanceof Error ? err.message : '工項讀取失敗' }
})
watch(profileKey, () => { materialId.value = ''; observed.value = '' })
watch(() => JSON.stringify([props.target, profileKey.value, materialId.value, observed.value, excludeProjectId.value]), () => {
  generation++; result.value = null; error.value = ''; loading.value = false
})
function optionalNumber(value: number | '') { return value === '' ? null : Number(value) }
async function estimate() {
  error.value = ''; result.value = null
  const t = props.target
  if (!materialId.value || !t.projectType || !t.city || !t.projectDate ||
      !t.totalFloorArea || t.totalFloorArea <= 0 || !t.siteArea || t.siteArea <= 0 ||
      !t.floorCount || t.floorCount <= 0 || t.basementFloorCount == null || t.basementFloorCount < 0) {
    error.value = '請先填妥目標建案條件，並明確選擇標準材料。'; return
  }
  if (!structureSupported.value) { error.value = '此工項不支援目前結構類型，請查看適用範圍。'; return }
  const payload = { profileKey: profileKey.value, materialId: Number(materialId.value), target: { ...t },
    observedQuantity: optionalNumber(observed.value),
    excludeProjectId: optionalNumber(excludeProjectId.value) }
  if ((payload.observedQuantity != null && (!Number.isFinite(payload.observedQuantity) || payload.observedQuantity < 0)) ||
      (payload.excludeProjectId != null && (!Number.isInteger(payload.excludeProjectId) || payload.excludeProjectId <= 0))) {
    error.value = '請確認 待比用量為非負數、排除建案 ID 為正整數。'; return
  }
  const current = ++generation
  loading.value = true
  try {
    const response = await api<Estimate>('/bulk-material-estimates/range', { method: 'POST', body: JSON.stringify(payload) })
    if (current === generation) result.value = response
  } catch (err) { if (current === generation) error.value = err instanceof Error ? err.message : '估算失敗' }
  finally { if (current === generation) loading.value = false }
}
function number(value: number | null, digits = 4) { return value == null ? '—' : value.toLocaleString('zh-TW', { maximumFractionDigits: digits }) }
</script>

<template>
  <div data-testid="bulk-material-estimate">
    <div class="bulk-body">
      <div v-if="optionsError" class="notice info">大宗工項目前無法載入：{{ optionsError }}。請確認已啟動新版後端。</div>
      <form v-else @submit.prevent="estimate">
        <div class="bulk-fields">
          <label class="field"><span>大宗工項</span><select v-model="profileKey" aria-label="大宗工項">
            <option v-for="p in profiles" :key="p.key" :value="p.key">{{ p.name }}</option>
          </select></label>
          <label class="field"><span>已對應標準材料（逐一估算，不自動合併）</span>
            <select v-model="materialId" aria-label="大宗標準材料" required>
              <option value="">請選擇</option>
              <option v-for="m in selected?.materials ?? []" :key="m.materialId" :value="m.materialId">{{ m.materialName }}（{{ m.unitName }}）</option>
            </select>
          </label>
          <label class="field"><span>待比總用量（選填，{{ selected?.unit }}）</span><input v-model.number="observed" aria-label="大宗待比用量" type="number" min="0" step="any" placeholder="請確認同材料與計量範圍" /></label>
          <label class="field"><span>排除歷史建案 ID（選填）</span><input v-model.number="excludeProjectId" aria-label="排除歷史建案 ID" type="number" min="1" step="1" placeholder="本案已在歷史庫時請排除" /></label>
        </div>
        <div v-if="selected" class="notice info mt-3">
          {{ selected.notice }}
          <div>適用結構：{{ selected.structures.join('、') }}；目前：{{ target.structureType || '尚未選擇' }}。單價與加工安裝費尚無一致口徑，這裡只估數量。</div>
          <div v-if="!selected.materials.length">目前沒有名稱與單位相符的啟用標準材料，請先確認材料對應。</div>
          <div v-if="selected.key === 'earthwork'">土方不合併不同開挖、外運或回填標準材料；地下層數必須大於零且與參考案相同。</div>
        </div>
        <button class="btn btn-primary mt-3" type="submit" :disabled="loading || !materialId || !structureSupported">{{ loading ? '估算中...' : '估算大宗工項／比對用量' }}</button>
      </form>
      <div v-if="error" class="notice error mt-3">{{ error }}</div>
      <div v-if="result" class="bulk-result" :class="{ review: result.status === 'ManualReviewRequired' }" data-testid="bulk-material-result">
        <h4>{{ result.materialName }}：{{ result.hasEnoughSamples ? '歷史參考區間' : '資料不足，尚無區間' }}</h4>
        <p>{{ result.message }}</p>
        <p class="text-muted small">候選 {{ result.candidateProjectCount }} 案 → 來源可用 {{ result.availableProjectCount }} 案 → 本次 {{ result.referenceProjectCount }} 案（最低 {{ result.minimumSampleCount }}）。
          ESS {{ number(result.effectiveSampleSize, 2) }}；最大權重占比 {{ number(result.largestWeightSharePercent, 2) }}%，不代表預測正確率。</p>
        <div v-if="result.recognizableSyntheticNameProjectCount" class="notice info">本次 {{ result.recognizableSyntheticNameProjectCount }} 個參考案名稱含合成標記，結果不能當作真實工程準確率。</div>
        <div v-if="result.hasEnoughSamples" class="bulk-ranges">
          <div><span>P{{ number(result.lowerQuantile * 100, 1) }} 下界</span><strong>{{ number(result.lowerQuantity) }} {{ result.unit }}</strong></div>
          <div><span>P{{ number(result.medianQuantile * 100, 1) }} 中位</span><strong>{{ number(result.medianQuantity) }} {{ result.unit }}</strong></div>
          <div><span>P{{ number(result.upperQuantile * 100, 1) }} 上界</span><strong>{{ number(result.upperQuantity) }} {{ result.unit }}</strong></div>
        </div>
        <p v-if="result.observedQuantity != null">待比用量：{{ number(result.observedQuantity) }} {{ result.unit }}。偏離僅提示人工複核，不顯示正式異常紅燈。</p>
        <ul><li v-for="warning in result.warnings" :key="warning">{{ warning }}</li></ul>
        <p class="text-muted small">{{ result.priceLimitation }}</p>
        <p class="text-muted small">用量來源為已核准、已對應的決標工項；已有歷史交易者會交叉核對，無需為新材料重跑回寫。</p>
        <details v-if="result.references.length">
          <summary>本次參考建案與來源列數</summary>
          <div class="table-wrap"><table>
            <thead><tr><th>建案／年月</th><th>結構</th><th>{{ result.basisLabel }}</th><th>總用量（{{ result.unit }}）</th><th>單位面積用量</th><th>來源列</th><th>相似度</th></tr></thead>
            <tbody><tr v-for="r in result.references" :key="r.projectId"><td>#{{ r.projectId }} {{ r.projectName }}<br />{{ r.projectDate.slice(0, 7) }}</td>
              <td>{{ r.structureType }}</td><td>{{ number(r.basisArea) }}</td><td>{{ number(r.quantity) }}</td><td>{{ number(r.usagePerArea) }} {{ result.unit }}/m²</td><td>{{ r.sourceRowCount }}<div v-if="r.finalBidOnlyRowCount" class="text-muted small">{{ r.finalBidOnlyRowCount }} 列直接取自決標</div></td><td>{{ number(r.similarityScore, 2) }}%</td></tr></tbody>
          </table></div>
        </details>
        <details v-if="result.excludedProjects.length"><summary>未納入的建案與原因（{{ result.excludedProjects.length }}）</summary>
          <ul><li v-for="p in result.excludedProjects" :key="p.projectId">#{{ p.projectId }}：{{ p.reason }}</li></ul>
        </details>
      </div>
    </div>
  </div>
</template>

<style scoped>
.bulk-body { padding: 20px; }
.bulk-fields { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }
.bulk-result { border: 1px solid #d7e3f3; border-radius: 10px; padding: 20px; margin-top: 20px; background: #f5f8fc; }
.bulk-result.review { border-color: #d9bb79; background: #fffaf0; }
.bulk-ranges { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin: 16px 0; }
.bulk-ranges > div { background: white; padding: 14px; border-radius: 8px; }
.bulk-ranges span, .bulk-ranges strong { display: block; }
.bulk-ranges span { color: #526274; font-size: 13px; }
.bulk-ranges strong { margin-top: 8px; font-variant-numeric: tabular-nums; }
.bulk-result details { margin-top: 14px; }
.bulk-result summary { cursor: pointer; }
.bulk-result table { width: 100%; min-width: 750px; }
.bulk-result th, .bulk-result td { padding: 10px; text-align: left; border-bottom: 1px solid #e5eaf0; vertical-align: top; }
@media (max-width: 800px) { .bulk-fields, .bulk-ranges { grid-template-columns: 1fr; } }
</style>

