<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../services/api'

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
const profiles = ref<Profile[]>([])
const profileError = ref('')
const profileKey = ref('')
const area = ref<number | null>(null)
const observed = ref<number | null>(null)
const estimating = ref(false)
const estimateError = ref('')
const result = ref<Estimate | null>(null)
const selectedProfile = computed(() => profiles.value.find(x => x.key === profileKey.value))
watch([profileKey, area, observed], () => { result.value = null; estimateError.value = '' })
onMounted(async () => {
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
  <section class="specification-estimate">
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
</template>

<style scoped>
.specification-estimate { padding: 20px; }
.specification-fields { display: grid; grid-template-columns: 1.5fr 1fr 1fr; gap: 16px; }
.specification-source { margin-top: 12px; color: #526274; font-size: 13px; }
.specification-result { padding: 18px; margin-top: 18px; background: #f0f5fb; border: 1px solid #d7e3f3; border-radius: 10px; }
.specification-result p { margin: 10px 0; }
.specification-result ul { padding-left: 20px; }
@media (max-width: 800px) { .specification-fields { grid-template-columns: 1fr; } }
</style>
