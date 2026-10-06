<script setup lang="ts">
import { computed, ref } from 'vue'

type StepState = 'idle' | 'loaded' | 'processed'

interface ProjectInfo {
  id: number
  name: string
  buildingType: string
  structureType: string
  location: string
  totalFloorArea: number
}

interface OneDItem {
  chapter: string
  code: string
  name: string
  unit: string
  quantity: number
}

interface TwoAItem extends OneDItem {
  detectionCategory?: string
  status: 'classified' | 'pending'
  note: string
}

const state = ref<StepState>('idle')
const selectedProjectId = ref(101)

const project: ProjectInfo = {
  id: 101,
  name: '集合住宅 A（POC）',
  buildingType: '集合住宅',
  structureType: 'RC',
  location: '台中市',
  totalFloorArea: 18650,
}

const sourceItems: OneDItem[] = [
  { chapter: '假設工程', code: 'TMP-001', name: '臨時水電及施工設施', unit: '式', quantity: 1 },
  { chapter: '土方及基礎工程', code: 'EAR-001', name: '地下室土方開挖', unit: 'M3', quantity: 8200 },
  { chapter: '結構體工程', code: 'STR-001', name: 'SD420W高拉力鋼筋#4以上', unit: 'KG', quantity: 100000 },
  { chapter: '結構體工程', code: 'STR-002', name: 'SD420W高拉力鋼筋#3以下', unit: 'KG', quantity: 40000 },
  { chapter: '結構體工程', code: 'STR-003', name: '預拌混凝土 3500psi', unit: 'M3', quantity: 3200 },
  { chapter: '結構體工程', code: 'STR-004', name: '結構體模板', unit: 'M2', quantity: 26000 },
  { chapter: '結構體工程', code: 'STR-005', name: 'SD420W鋼筋續接器', unit: '個', quantity: 780 },
  { chapter: '鋼骨鋼構工程', code: 'STL-001', name: 'H型鋼鋼柱', unit: 'T', quantity: 850 },
  { chapter: '防水工程', code: 'WAT-001', name: '屋頂防水層', unit: 'M2', quantity: 1800 },
  { chapter: '門窗工程', code: 'DOR-001', name: '鋁門窗', unit: '樘', quantity: 126 },
  { chapter: '景觀工程', code: 'LND-001', name: '景觀植栽', unit: '式', quantity: 1 },
]

function classifyItem(item: OneDItem): TwoAItem {
  if (item.chapter === '結構體工程') {
    if (item.name.includes('續接器') || item.name.includes('套筒') || item.name.includes('接頭')) {
      return {
        ...item,
        status: 'pending',
        note: '鋼筋相關配件，暫不直接併入鋼筋重量',
      }
    }

    if (
      item.name.includes('鋼筋') ||
      item.name.includes('SD420') ||
      item.name.includes('SD280')
    ) {
      return {
        ...item,
        detectionCategory: '鋼筋',
        status: 'classified',
        note: '沿用 1D 大項「結構體工程」，2A 再往下歸入「鋼筋」',
      }
    }

    if (item.name.includes('混凝土') || item.name.includes('預拌')) {
      return {
        ...item,
        detectionCategory: '混凝土',
        status: 'classified',
        note: '沿用 1D 大項「結構體工程」，2A 再往下歸入「混凝土」',
      }
    }

    if (item.name.includes('模板') || item.name.includes('模版')) {
      return {
        ...item,
        detectionCategory: '模板',
        status: 'classified',
        note: '沿用 1D 大項「結構體工程」，2A 再往下歸入「模板」',
      }
    }
  }

  if (item.chapter === '鋼骨鋼構工程') {
    if (
      item.name.includes('鋼骨') ||
      item.name.includes('鋼構') ||
      item.name.includes('H型鋼') ||
      item.name.includes('鋼柱')
    ) {
      return {
        ...item,
        detectionCategory: '鋼骨',
        status: 'classified',
        note: '沿用 1D 大項「鋼骨鋼構工程」，2A 再往下歸入「鋼骨」',
      }
    }
  }

  return {
    ...item,
    status: 'pending',
    note: '1D 大項已保留，2A 細項規則尚待後續建立',
  }
}

const processedItems = computed(() => sourceItems.map(classifyItem))

const chapterSummary = computed(() => {
  const map = new Map<string, number>()
  for (const item of sourceItems) {
    map.set(item.chapter, (map.get(item.chapter) ?? 0) + 1)
  }
  return Array.from(map.entries()).map(([chapter, count]) => ({ chapter, count }))
})

const categorySummary = computed(() => {
  const map = new Map<string, TwoAItem[]>()
  for (const item of processedItems.value) {
    if (!item.detectionCategory) continue
    const key = `${item.chapter}|${item.detectionCategory}`
    const list = map.get(key) ?? []
    list.push(item)
    map.set(key, list)
  }

  return Array.from(map.entries()).map(([key, items]) => {
    const [chapter, category] = key.split('|')
    const units = Array.from(new Set(items.map((item) => item.unit)))
    const canAggregate = units.length === 1

    return {
      chapter,
      category,
      itemCount: items.length,
      unit: canAggregate ? units[0] : '需換算',
      total: canAggregate ? items.reduce((sum, item) => sum + item.quantity, 0) : null,
    }
  })
})

const classifiedCount = computed(
  () => processedItems.value.filter((item) => item.status === 'classified').length,
)

const pendingCount = computed(() => processedItems.value.length - classifiedCount.value)

function loadOneDResult() {
  state.value = 'loaded'
}

function runTwoAPreprocess() {
  state.value = 'processed'
}

function resetDemo() {
  state.value = 'idle'
}

function formatNumber(value: number | null) {
  if (value === null) return '-'
  return value.toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}
</script>

<template>
  <div class="page">
    <div class="page-header">
      <div>
        <h2>2A 量價異常偵測－執行預覽</h2>
        <p>POC 畫面：模擬 2A 取得 1D 已完成的整個建案工程大項後，再進行細項整理與後續異常分析前處理。</p>
      </div>
      <span class="poc-badge">POC</span>
    </div>

    <section class="card">
      <div class="card-title">建案選擇</div>
      <div class="project-row">
        <div class="field">
          <label>建案</label>
          <select v-model="selectedProjectId" class="form-control">
            <option :value="101">{{ project.name }}</option>
          </select>
        </div>
        <div class="project-meta">
          <span><strong>類型：</strong>{{ project.buildingType }}</span>
          <span><strong>構造：</strong>{{ project.structureType }}</span>
          <span><strong>地區：</strong>{{ project.location }}</span>
          <span><strong>總樓地板面積：</strong>{{ formatNumber(project.totalFloorArea) }} m²</span>
        </div>
        <div class="actions">
          <button class="btn primary" type="button" @click="loadOneDResult">
            讀取 1D 分類結果
          </button>
          <button v-if="state !== 'idle'" class="btn secondary" type="button" @click="resetDemo">
            重設
          </button>
        </div>
      </div>
    </section>

    <section v-if="state !== 'idle'" class="card">
      <div class="card-header">
        <div>
          <div class="card-title">1D 已完成的工程大項</div>
          <div class="card-desc">2A 直接承接，不重新判斷工程大項。</div>
        </div>
        <button
          class="btn primary"
          type="button"
          :disabled="state === 'processed'"
          @click="runTwoAPreprocess"
        >
          {{ state === 'processed' ? '2A 前處理完成' : '執行 2A 細項整理' }}
        </button>
      </div>

      <div class="chapter-grid">
        <article v-for="chapter in chapterSummary" :key="chapter.chapter" class="chapter-card">
          <div class="chapter-name">{{ chapter.chapter }}</div>
          <div class="chapter-count">{{ chapter.count }} 筆工項</div>
        </article>
      </div>
    </section>

    <template v-if="state === 'processed'">
      <section class="summary-grid">
        <article class="summary-card">
          <div class="summary-label">1D 工程大項</div>
          <div class="summary-value">{{ chapterSummary.length }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">建案工項</div>
          <div class="summary-value">{{ sourceItems.length }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">已建立 2A 細項</div>
          <div class="summary-value">{{ classifiedCount }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">待後續定義</div>
          <div class="summary-value">{{ pendingCount }}</div>
        </article>
      </section>

      <section class="card">
        <div class="card-title">2A 細項彙整結果</div>
        <div class="card-desc">
          目前先示範結構體工程與鋼骨鋼構工程；其他 1D 大項仍保留，之後再逐步建立細項規則。
        </div>

        <div class="result-grid">
          <article v-for="group in categorySummary" :key="`${group.chapter}-${group.category}`" class="result-card">
            <div class="result-path">
              <span>{{ group.chapter }}</span>
              <span class="arrow">→</span>
              <strong>{{ group.category }}</strong>
            </div>
            <div class="result-meta">{{ group.itemCount }} 筆原始工項</div>
            <div class="result-total">
              {{ formatNumber(group.total) }} {{ group.unit }}
            </div>
            <div class="result-note">後續將以此層級接相似建案與數量異常判定。</div>
          </article>
        </div>
      </section>

      <section class="card">
        <div class="card-title">建案工項明細</div>
        <div class="card-desc">保留「1D 工程大項 → 2A 偵錯細項 → 原始工項」三層追溯。</div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>1D 工程大項</th>
                <th>2A 偵錯細項</th>
                <th>原始工項</th>
                <th>單位</th>
                <th>數量</th>
                <th>狀態</th>
                <th>說明</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in processedItems" :key="item.code">
                <td class="strong">{{ item.chapter }}</td>
                <td>{{ item.detectionCategory ?? '—' }}</td>
                <td class="left strong">{{ item.name }}</td>
                <td>{{ item.unit }}</td>
                <td class="number">{{ formatNumber(item.quantity) }}</td>
                <td>
                  <span class="status" :class="item.status">
                    {{ item.status === 'classified' ? '已分類' : '待定義' }}
                  </span>
                </td>
                <td class="left note">{{ item.note }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="next-step-card">
        <div>
          <strong>下一階段（尚未實作）</strong>
          <p>將細項彙整結果接回既有相似建案、單位標準化、合理範圍與異常判定流程。</p>
        </div>
        <button class="btn disabled-btn" type="button" disabled>執行量價異常偵測</button>
      </section>
    </template>

    <section v-else-if="state === 'idle'" class="empty-state">
      請先選擇建案並讀取 1D 分類結果。
    </section>
  </div>
</template>

<style scoped>
.page { max-width: 1180px; margin: 0 auto; padding: 22px 0 42px; }
.page-header { display:flex; justify-content:space-between; align-items:flex-start; gap:16px; margin-bottom:16px; }
.page-header h2 { margin:0; font-size:1.55rem; font-weight:800; color:#1f2d3d; }
.page-header p { margin:6px 0 0; color:#6c757d; font-size:13px; }
.poc-badge { padding:5px 10px; border-radius:999px; background:#fff3cd; color:#664d03; font-size:12px; font-weight:800; }
.card { background:#fff; border:1px solid #d8dbe0; border-radius:8px; box-shadow:0 3px 12px rgba(20,29,35,.06); padding:18px; margin-bottom:16px; }
.card-header { display:flex; align-items:center; justify-content:space-between; gap:16px; }
.card-title { font-size:15px; font-weight:800; color:#1f2d3d; }
.card-desc { margin-top:4px; color:#6c757d; font-size:12px; line-height:1.6; }
.project-row { display:grid; grid-template-columns:minmax(240px,1fr) 2fr auto; gap:18px; align-items:end; }
.field label { display:block; margin-bottom:6px; color:#495057; font-size:12px; font-weight:700; }
.form-control { width:100%; border:1px solid #ced4da; border-radius:6px; padding:9px 10px; font-size:14px; background:#fff; }
.project-meta { display:flex; flex-wrap:wrap; gap:8px 18px; color:#495057; font-size:12px; padding-bottom:7px; }
.actions { display:flex; gap:8px; }
.btn { border:0; border-radius:6px; padding:9px 14px; font-size:13px; font-weight:800; cursor:pointer; }
.btn.primary { background:#321fdb; color:#fff; }
.btn.secondary { background:#e9ecef; color:#343a40; }
.btn:disabled { opacity:.55; cursor:not-allowed; }
.chapter-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(180px,1fr)); gap:10px; margin-top:16px; }
.chapter-card { border:1px solid #dee2e6; border-left:4px solid #321fdb; border-radius:7px; padding:12px 13px; background:#fff; }
.chapter-name { color:#1f2d3d; font-weight:800; font-size:14px; }
.chapter-count { margin-top:5px; color:#6c757d; font-size:11px; }
.summary-grid { display:grid; grid-template-columns:repeat(4,minmax(0,1fr)); gap:12px; margin-bottom:16px; }
.summary-card { background:#fff; border:1px solid #d8dbe0; border-radius:8px; padding:15px 17px; }
.summary-label { color:#6c757d; font-size:12px; }
.summary-value { margin-top:4px; color:#1f2d3d; font-size:25px; font-weight:900; }
.result-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(245px,1fr)); gap:12px; margin-top:16px; }
.result-card { border:1px solid #d8dbe0; border-radius:8px; padding:14px; background:#fff; }
.result-path { display:flex; align-items:center; gap:7px; color:#6c757d; font-size:12px; }
.result-path strong { color:#1f2d3d; font-size:17px; }
.arrow { color:#adb5bd; }
.result-meta { margin-top:8px; color:#6c757d; font-size:11px; }
.result-total { margin-top:6px; color:#0f5132; font-size:19px; font-weight:900; }
.result-note { margin-top:8px; color:#6c757d; font-size:11px; line-height:1.5; }
.table-wrap { margin-top:14px; overflow-x:auto; border:1px solid #dee2e6; border-radius:7px; }
table { width:100%; min-width:1120px; border-collapse:collapse; font-size:12px; }
th { background:#5b6065; color:#fff; padding:9px 8px; text-align:center; white-space:nowrap; }
td { padding:9px 8px; border-bottom:1px solid #edf0f2; text-align:center; vertical-align:middle; }
.left { text-align:left; }
.strong { font-weight:800; color:#1f2d3d; }
.number { text-align:right; font-variant-numeric:tabular-nums; }
.note { min-width:290px; color:#6c757d; }
.status { display:inline-block; padding:4px 8px; border-radius:5px; font-size:11px; font-weight:800; }
.status.classified { background:#d1e7dd; color:#0f5132; }
.status.pending { background:#fff3cd; color:#664d03; }
.next-step-card { display:flex; align-items:center; justify-content:space-between; gap:18px; background:#f8f9fa; border:1px dashed #adb5bd; border-radius:8px; padding:16px 18px; }
.next-step-card strong { color:#1f2d3d; font-size:14px; }
.next-step-card p { margin:4px 0 0; color:#6c757d; font-size:12px; }
.disabled-btn { background:#adb5bd; color:#fff; }
.empty-state { padding:46px; text-align:center; color:#adb5bd; border:1px dashed #ced4da; border-radius:8px; background:#fff; }
@media (max-width:900px) {
  .project-row { grid-template-columns:1fr; }
  .summary-grid { grid-template-columns:repeat(2,minmax(0,1fr)); }
  .next-step-card { align-items:stretch; flex-direction:column; }
}
</style>
