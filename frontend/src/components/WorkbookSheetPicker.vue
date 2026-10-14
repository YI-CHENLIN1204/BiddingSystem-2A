<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { apiForm } from '../services/api'

interface SheetPreview {
  name: string
  itemCount: number
  sampleItems: string[]
  recommendation: string
  reason: string
}
interface PreviewResult { fileName: string; sheets: SheetPreview[]; message: string }

type CandidateType = 'Rebar' | 'Concrete' | 'Excluded' | 'Other'
interface InspectionRow {
  sheetName: string
  rowNumber: number
  itemCode: string
  itemName: string
  spec: string
  unit: string
  qty: number | null
  unitPrice: number | null
  amount: number | null
  category1: string
  category2: string
  category3: string
  effectiveMajorCategory: string | null
  candidateType: CandidateType
  standardQuantity: number | null
  standardUnit: string | null
  warnings: string[]
}
interface QuantitySummary {
  candidateType: CandidateType
  label: string
  unit: string
  candidateRowCount: number
  convertedRowCount: number
  reviewRowCount: number
  candidateQuantity: number
}
interface InspectionResult {
  totalRows: number
  warningRows: number
  quantitySummaries: QuantitySummary[]
  rows: InspectionRow[]
  notice: string
}

const props = defineProps<{ file: File | null; modelValue: string[] }>()
const emit = defineEmits<{
  'update:modelValue': [value: string[]]
  'update:ready': [value: boolean]
}>()

const sheets = ref<SheetPreview[]>([])
const loading = ref(false)
const error = ref('')
const inspection = ref<InspectionResult | null>(null)
const inspectionError = ref('')
const inspecting = ref(false)
const filter = ref<'All' | 'Rebar' | 'Concrete' | 'Review'>('All')
let requestId = 0
let inspectionRequestId = 0

const shownRows = computed(() => {
  const rows = inspection.value?.rows ?? []
  if (filter.value === 'All') return rows
  if (filter.value === 'Review') return rows.filter(row => row.warnings.length > 0)
  return rows.filter(row => row.candidateType === filter.value)
})

watch(() => props.file, async (file) => {
  const id = ++requestId
  ++inspectionRequestId
  sheets.value = []
  inspection.value = null
  inspectionError.value = ''
  error.value = ''
  emit('update:modelValue', [])
  emit('update:ready', false)
  if (!file) return

  loading.value = true
  try {
    const data = new FormData()
    data.append('excelFile', file)
    const result = await apiForm<PreviewResult>('/tender-import/preview', data)
    if (id !== requestId) return
    sheets.value = result.sheets
    // 多工作表不自動選取；單工作表仍須確認來源。
    if (result.sheets.length === 1 && result.sheets[0].itemCount > 0)
      emit('update:modelValue', [result.sheets[0].name])
    emit('update:ready', true)
  } catch (caught) {
    if (id === requestId)
      error.value = caught instanceof Error ? caught.message : '無法預覽 Excel 工作表'
  } finally {
    if (id === requestId) loading.value = false
  }
}, { immediate: true })

watch(() => props.modelValue.join('\u001f'), () => {
  ++inspectionRequestId
  inspection.value = null
  inspectionError.value = ''
  filter.value = 'All'
})

function toggle(name: string, checked: boolean) {
  const current = props.modelValue
  emit('update:modelValue', checked ? [...current, name] : current.filter(x => x !== name))
}

async function inspectSelected() {
  if (!props.file || !props.modelValue.length) return
  const id = ++inspectionRequestId
  const originalFile = props.file
  const selected = [...props.modelValue]
  inspecting.value = true
  inspectionError.value = ''
  inspection.value = null
  try {
    const data = new FormData()
    data.append('excelFile', originalFile)
    data.append('includedSheetNames', JSON.stringify(selected))
    const result = await apiForm<InspectionResult>('/tender-import/inspect', data)
    if (id !== inspectionRequestId || originalFile !== props.file) return
    inspection.value = result
    filter.value = 'All'
  } catch (caught) {
    if (id === inspectionRequestId)
      inspectionError.value = caught instanceof Error ? caught.message : '無法檢查候選明細'
  } finally {
    if (id === inspectionRequestId) inspecting.value = false
  }
}

function number(value: number | null | undefined) {
  if (value === null || value === undefined) return '—'
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 3 }).format(value)
}

function typeName(type: CandidateType) {
  if (type === 'Rebar') return '鋼筋候選'
  if (type === 'Concrete') return '混凝土候選'
  if (type === 'Excluded') return '輔助項排除'
  return '其他工項'
}
</script>

<template>
  <section v-if="file" class="workbook-review mt-3 p-3 border rounded bg-light">
    <div class="fw-bold mb-2">上傳前檢查：選擇真正的工程明細工作表</div>
    <p class="small text-muted mb-2">
      只會匯入勾選的工作表。總表、說明、廠商詢價與比價資料不可直接當成決標工程明細。
      工作表候選筆數不代表數量、價格都正確；可先檢查每筆明細與鋼筋／混凝土候選用量，不必先建立建案。
    </p>
    <div v-if="loading" class="text-muted small">正在預覽工作表，請稍候...</div>
    <div v-if="error" class="alert alert-danger py-2">{{ error }}</div>
    <div v-if="!loading && !error && sheets.length" class="d-grid gap-2">
      <label v-for="sheet in sheets" :key="sheet.name"
        class="d-flex align-items-start gap-2 border rounded p-2 bg-white"
        :class="{ 'opacity-50': sheet.itemCount === 0 }">
        <input type="checkbox" class="form-check-input mt-1" :checked="modelValue.includes(sheet.name)"
          :disabled="sheet.itemCount === 0"
          @change="toggle(sheet.name, ($event.target as HTMLInputElement).checked)" />
        <span class="flex-grow-1">
          <span class="fw-semibold">{{ sheet.name }}</span>
          <span class="small text-muted ms-2">候選明細 {{ sheet.itemCount }} 筆</span>
          <span class="small d-block text-muted">{{ sheet.reason }}</span>
          <span v-if="sheet.sampleItems.length" class="small d-block text-muted">
            範例：{{ sheet.sampleItems.join('、') }}
          </span>
        </span>
      </label>
      <div class="small fw-semibold" :class="modelValue.length ? 'text-success' : 'text-danger'">
        已選 {{ modelValue.length }} 張工作表。
        {{ modelValue.length ? '請確認全數屬於同一工程及相同階段的正式明細。' : '請至少勾選一張真正的工程明細工作表。' }}
      </div>
      <div class="d-flex gap-2 align-items-center flex-wrap mt-2">
        <button type="button" class="btn btn-sm btn-outline-primary"
          :disabled="!modelValue.length || inspecting" @click="inspectSelected">
          {{ inspecting ? '檢查中...' : '檢查選取工作表的全部候選明細' }}
        </button>
        <span class="small text-muted">唯讀檢查，不建立案件、不回寫歷史，也不需填寫建案基本資料。</span>
      </div>
      <div v-if="inspectionError" class="alert alert-danger py-2 mt-1">{{ inspectionError }}</div>
      <div v-if="inspection" class="mt-2 bg-white rounded border p-3">
        <h6 class="fw-bold mb-2">候選量價明細與數量預檢</h6>
        <p class="small text-warning-emphasis mb-3">{{ inspection.notice }}</p>
        <div class="row g-2 mb-3">
          <div v-for="summary in inspection.quantitySummaries" :key="summary.candidateType" class="col-md-6">
            <div class="border rounded p-2 h-100">
              <div class="small text-muted">{{ summary.label }}候選合計（未審核）</div>
              <div class="fw-bold fs-5">{{ number(summary.candidateQuantity) }} {{ summary.unit }}</div>
              <div class="small text-muted">
                候選 {{ summary.candidateRowCount }} 筆，成功換算 {{ summary.convertedRowCount }} 筆；
                其中 {{ summary.reviewRowCount }} 筆有警示
              </div>
            </div>
          </div>
        </div>
        <div class="small text-muted mb-2">
          共 {{ inspection.totalRows }} 筆候選明細，{{ inspection.warningRows }} 筆有注意事項。
          可選擇鋼筋、混凝土或需檢視列，逐筆核對 Excel 來源。
        </div>
        <div class="d-flex gap-2 flex-wrap mb-2">
          <button v-for="choice in [
            { key: 'All', name: '全部' }, { key: 'Rebar', name: '鋼筋' },
            { key: 'Concrete', name: '混凝土' }, { key: 'Review', name: '需要檢視' }
          ]" :key="choice.key" type="button" class="btn btn-sm"
            :class="filter === choice.key ? 'btn-primary' : 'btn-outline-secondary'"
            @click="filter = choice.key as 'All' | 'Rebar' | 'Concrete' | 'Review'">
            {{ choice.name }}
          </button>
        </div>
        <div class="table-responsive" style="max-height: 430px; overflow-y: auto;">
          <table class="table table-sm table-striped table-bordered align-middle small">
            <thead class="table-light" style="position: sticky; top: 0; z-index: 1;">
              <tr>
                <th>來源</th><th>工項／規格</th><th>工程大項</th><th>辨識</th>
                <th class="text-end">原始數量</th><th>單位</th>
                <th class="text-end">單價</th><th class="text-end">複價</th>
                <th class="text-end">候選標準量</th><th>注意事項</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in shownRows" :key="row.sheetName + ':' + row.rowNumber">
                <td class="text-nowrap">{{ row.sheetName }} 第 {{ row.rowNumber }} 列</td>
                <td style="min-width: 200px;">{{ row.itemName }}<div v-if="row.spec" class="text-muted">{{ row.spec }}</div></td>
                <td class="text-nowrap">{{ row.effectiveMajorCategory ?? '未辨識' }}</td>
                <td class="text-nowrap">{{ typeName(row.candidateType) }}</td>
                <td class="text-end">{{ number(row.qty) }}</td>
                <td>{{ row.unit || '—' }}</td>
                <td class="text-end">{{ number(row.unitPrice) }}</td>
                <td class="text-end">{{ number(row.amount) }}</td>
                <td class="text-end text-nowrap">
                  {{ number(row.standardQuantity) }} {{ row.standardQuantity === null ? '' : row.standardUnit }}
                </td>
                <td style="min-width: 200px;">
                  <span v-if="!row.warnings.length" class="text-muted">—</span>
                  <div v-for="warning in row.warnings" :key="warning" class="text-warning-emphasis">{{ warning }}</div>
                </td>
              </tr>
              <tr v-if="!shownRows.length">
                <td colspan="10" class="text-center text-muted">此篩選條件沒有候選明細</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="small text-muted mt-2 mb-0">
          未被歸類為鋼筋／混凝土的工項仍會顯示，但不加入候選用量。疑似續接器、加工或澆置等輔助項目不自動計重；
          同工項重複、工程大項及單位問題仍須人工確認。這不是 P15/P50/P85 異常判定。
        </p>
      </div>
    </div>
  </section>
</template>
