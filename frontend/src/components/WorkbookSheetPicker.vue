<script setup lang="ts">
import { ref, watch } from 'vue'
import { apiForm } from '../services/api'

interface SheetPreview {
  name: string
  itemCount: number
  sampleItems: string[]
  recommendation: string
  reason: string
}
interface PreviewResult { fileName: string; sheets: SheetPreview[]; message: string }

const props = defineProps<{ file: File | null; modelValue: string[] }>()
const emit = defineEmits<{
  'update:modelValue': [value: string[]]
  'update:ready': [value: boolean]
}>()

const sheets = ref<SheetPreview[]>([])
const loading = ref(false)
const error = ref('')
let requestId = 0

watch(() => props.file, async (file) => {
  const id = ++requestId
  sheets.value = []
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
    // 單一工作表仍列出讓使用者確認；多工作表必須手動挑選，避免混入詢價／比較表。
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

function toggle(name: string, checked: boolean) {
  const current = props.modelValue
  emit('update:modelValue', checked ? [...current, name] : current.filter(x => x !== name))
}
</script>

<template>
  <section v-if="file" class="workbook-review mt-3 p-3 border rounded bg-light">
    <div class="fw-bold mb-2">上傳前檢查：選擇真正的工程明細工作表</div>
    <p class="small text-muted mb-2">
      只會匯入勾選的工作表。總表、說明、廠商詢價與比價資料不可直接當成決標工程明細。
      此處僅顯示可解析的候選筆數，不代表每筆數量、價格都已正確，匯入後仍須審核。
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
    </div>
  </section>
</template>
