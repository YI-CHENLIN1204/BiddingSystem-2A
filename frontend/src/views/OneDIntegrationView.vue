<script setup lang="ts">
import { computed, ref } from 'vue'

interface DemoItem {
  chapter: string
  code: string
  name: string
  unit: string
  quantity: number
  detectionCategory?: string
  status: 'ready' | 'pending'
  note: string
}

interface MajorCategory {
  name: string
  itemCount: number
  detectionCategories: string[]
  status: 'ready' | 'pending'
}

const started = ref(false)

const items: DemoItem[] = [
  {
    chapter: '假設工程',
    code: 'TMP-001',
    name: '臨時水電及施工設施',
    unit: '式',
    quantity: 1,
    status: 'pending',
    note: '1D 已完成工程大項分類；2A 細項規則尚未建立',
  },
  {
    chapter: '土方及基礎工程',
    code: 'EAR-001',
    name: '地下室土方開挖',
    unit: 'M3',
    quantity: 8200,
    status: 'pending',
    note: '1D 已完成工程大項分類；2A 細項規則尚未建立',
  },
  {
    chapter: '結構體工程',
    code: 'STR-001',
    name: 'SD420W高拉力鋼筋#4以上',
    unit: 'KG',
    quantity: 100000,
    detectionCategory: '鋼筋',
    status: 'ready',
    note: '1D 大項：結構體工程 → 2A 偵錯細項：鋼筋',
  },
  {
    chapter: '結構體工程',
    code: 'STR-002',
    name: 'SD420W高拉力鋼筋#3以下',
    unit: 'KG',
    quantity: 40000,
    detectionCategory: '鋼筋',
    status: 'ready',
    note: '1D 大項：結構體工程 → 2A 偵錯細項：鋼筋',
  },
  {
    chapter: '結構體工程',
    code: 'STR-003',
    name: '預拌混凝土 3500psi',
    unit: 'M3',
    quantity: 3200,
    detectionCategory: '混凝土',
    status: 'ready',
    note: '1D 大項：結構體工程 → 2A 偵錯細項：混凝土',
  },
  {
    chapter: '結構體工程',
    code: 'STR-004',
    name: '結構體模板',
    unit: 'M2',
    quantity: 26000,
    detectionCategory: '模板',
    status: 'ready',
    note: '1D 大項：結構體工程 → 2A 偵錯細項：模板',
  },
  {
    chapter: '結構體工程',
    code: 'STR-005',
    name: 'SD420W鋼筋續接器',
    unit: '個',
    quantity: 780,
    status: 'pending',
    note: '鋼筋相關配件先保留待確認，不直接併入鋼筋重量',
  },
  {
    chapter: '鋼骨鋼構工程',
    code: 'STL-001',
    name: 'H型鋼鋼柱',
    unit: 'T',
    quantity: 850,
    detectionCategory: '鋼骨',
    status: 'ready',
    note: '1D 大項：鋼骨鋼構工程 → 2A 偵錯細項：鋼骨',
  },
  {
    chapter: '防水工程',
    code: 'WAT-001',
    name: '屋頂防水層',
    unit: 'M2',
    quantity: 1800,
    status: 'pending',
    note: '1D 已完成工程大項分類；2A 細項規則尚未建立',
  },
  {
    chapter: '門窗工程',
    code: 'DOR-001',
    name: '鋁門窗',
    unit: '樘',
    quantity: 126,
    status: 'pending',
    note: '1D 已完成工程大項分類；2A 細項規則尚未建立',
  },
  {
    chapter: '景觀工程',
    code: 'LND-001',
    name: '景觀植栽',
    unit: '式',
    quantity: 1,
    status: 'pending',
    note: '1D 已完成工程大項分類；2A 細項規則尚未建立',
  },
]

const majorCategories = computed<MajorCategory[]>(() => {
  const map = new Map<string, DemoItem[]>()

  for (const item of items) {
    const group = map.get(item.chapter) ?? []
    group.push(item)
    map.set(item.chapter, group)
  }

  return Array.from(map.entries()).map(([name, group]) => {
    const detectionCategories = Array.from(
      new Set(group.map((item) => item.detectionCategory).filter(Boolean) as string[]),
    )

    return {
      name,
      itemCount: group.length,
      detectionCategories,
      status: detectionCategories.length > 0 ? 'ready' : 'pending',
    }
  })
})

const detectionGroups = computed(() => {
  const map = new Map<string, DemoItem[]>()

  for (const item of items) {
    if (!item.detectionCategory) continue
    const key = `${item.chapter}|${item.detectionCategory}`
    const group = map.get(key) ?? []
    group.push(item)
    map.set(key, group)
  }

  return Array.from(map.entries()).map(([key, group]) => {
    const [chapter, detectionCategory] = key.split('|')
    const units = Array.from(new Set(group.map((item) => item.unit)))
    const canAggregate = units.length === 1
    const totalQuantity = canAggregate
      ? group.reduce((sum, item) => sum + item.quantity, 0)
      : null

    return {
      chapter,
      detectionCategory,
      itemCount: group.length,
      units,
      totalQuantity,
      canAggregate,
    }
  })
})

const classifiedItems = computed(() => items.filter((item) => item.status === 'ready').length)
const pendingItems = computed(() => items.length - classifiedItems.value)

function formatNumber(value: number | null) {
  if (value === null) return '-'
  return value.toLocaleString('zh-TW', { maximumFractionDigits: 2 })
}

function startDemo() {
  started.value = true
}
</script>

<template>
  <div class="poc-page">
    <section class="hero-card">
      <div>
        <div class="eyebrow">REPORT POC · 不影響正式系統</div>
        <h2>1D → 2A 建案資料流可視化</h2>
        <p>
          此頁只用於今天進度報告。以「一個建案」為單位，展示 1D 已完成工程大項分類後，
          2A 如何承接整個建案並往下建立偵錯細項。此 POC 不寫入資料庫、不呼叫正式異常偵測流程。
        </p>
      </div>
      <button class="primary-button" type="button" @click="startDemo">
        {{ started ? '重新播放 POC' : '開始 POC 展示' }}
      </button>
    </section>

    <section class="flow-card">
      <div class="flow-step">
        <div class="step-no">1</div>
        <div>
          <strong>建案標單</strong>
          <span>同一 Project 的完整工項資料</span>
        </div>
      </div>
      <div class="flow-arrow">→</div>
      <div class="flow-step active">
        <div class="step-no">2</div>
        <div>
          <strong>1D 工程大項分類</strong>
          <span>結構體、防水、門窗、鋼骨鋼構…</span>
        </div>
      </div>
      <div class="flow-arrow">→</div>
      <div class="flow-step active">
        <div class="step-no">3</div>
        <div>
          <strong>2A 偵錯細項</strong>
          <span>鋼筋、混凝土、模板、鋼骨…</span>
        </div>
      </div>
      <div class="flow-arrow">→</div>
      <div class="flow-step muted">
        <div class="step-no">4</div>
        <div>
          <strong>量價異常分析</strong>
          <span>下一階段接現有模型</span>
        </div>
      </div>
    </section>

    <template v-if="started">
      <section class="summary-grid">
        <article class="summary-card">
          <div class="summary-label">示範建案</div>
          <div class="summary-value small">集合住宅 A</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">1D 工程大項</div>
          <div class="summary-value">{{ majorCategories.length }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">原始工項</div>
          <div class="summary-value">{{ items.length }}</div>
        </article>
        <article class="summary-card">
          <div class="summary-label">2A 已建立細項</div>
          <div class="summary-value">{{ classifiedItems }}</div>
        </article>
      </section>

      <section class="content-card">
        <div class="card-heading">
          <div>
            <div class="section-index">POC 01</div>
            <h3>一個建案內的 1D 工程大項</h3>
          </div>
          <p>所有工程大項都由 1D 提供；2A 不重新做大項分類。</p>
        </div>

        <div class="major-grid">
          <article
            v-for="major in majorCategories"
            :key="major.name"
            class="major-card"
            :class="major.status"
          >
            <div class="major-head">
              <span class="source-badge">1D</span>
              <span
                class="state-badge"
                :class="major.status === 'ready' ? 'green' : 'yellow'"
              >
                {{ major.status === 'ready' ? '已有 2A 細項' : '保留大項' }}
              </span>
            </div>
            <div class="major-name">{{ major.name }}</div>
            <div class="major-count">{{ major.itemCount }} 筆工項</div>

            <div v-if="major.detectionCategories.length" class="tag-list">
              <span
                v-for="category in major.detectionCategories"
                :key="category"
                class="detail-tag"
              >
                {{ category }}
              </span>
            </div>
            <div v-else class="pending-note">
              目前尚未建立 2A 偵錯細項規則，但資料不會遺失。
            </div>
          </article>
        </div>
      </section>

      <section class="content-card">
        <div class="card-heading">
          <div>
            <div class="section-index">POC 02</div>
            <h3>2A 目前建立的偵錯細項</h3>
          </div>
          <p>細項只是 2A 的分析層；1D 原本的大項與原始工項全部保留。</p>
        </div>

        <div class="group-grid">
          <article
            v-for="group in detectionGroups"
            :key="`${group.chapter}-${group.detectionCategory}`"
            class="group-card"
          >
            <div class="group-path">
              <span>{{ group.chapter }}</span>
              <span class="path-arrow">→</span>
              <strong>{{ group.detectionCategory }}</strong>
            </div>
            <div class="group-meta">
              {{ group.itemCount }} 筆原始工項 · 單位 {{ group.units.join('、') }}
            </div>
            <div v-if="group.canAggregate" class="group-total">
              POC 彙整：{{ formatNumber(group.totalQuantity) }} {{ group.units[0] }}
            </div>
            <div v-else class="group-warning">
              單位不一致，正式分析前需先標準化。
            </div>
          </article>
        </div>
      </section>

      <section class="content-card">
        <div class="card-heading">
          <div>
            <div class="section-index">POC 03</div>
            <h3>工項追溯</h3>
          </div>
          <p>展示「工程大項 → 偵錯細項 → 原始工項」三層資料仍可回查。</p>
        </div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>1D 工程大項</th>
                <th>2A 偵錯細項</th>
                <th>原始工項 / 規格</th>
                <th>單位</th>
                <th>數量</th>
                <th>POC 狀態</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in items" :key="item.code">
                <td class="strong-cell">{{ item.chapter }}</td>
                <td>{{ item.detectionCategory ?? '—' }}</td>
                <td class="left-cell">
                  <div class="item-name">{{ item.name }}</div>
                  <div class="item-note">{{ item.note }}</div>
                </td>
                <td>{{ item.unit }}</td>
                <td class="number-cell">{{ formatNumber(item.quantity) }}</td>
                <td>
                  <span
                    class="state-badge"
                    :class="item.status === 'ready' ? 'green' : 'yellow'"
                  >
                    {{ item.status === 'ready' ? '已建立細項' : '待後續定義' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="report-note">
        <strong>今天可以報告的結論：</strong>
        1D 已負責「整個建案」的工程大項分類；2A POC 已完成建案層級資料承接與細項分類可視化。
        目前僅驗證資料層級與分類方向，尚未修改正式異常偵測系統。
        <span>待會議確認細項粒度與 1D / 2A 操作流程後，再進入正式串接。</span>
      </section>
    </template>

    <section v-else class="empty-preview">
      按下「開始 POC 展示」後，畫面才展開示範建案與 1D → 2A 分類結果。
    </section>
  </div>
</template>

<style scoped>
.poc-page {
  max-width: 1180px;
  margin: 0 auto;
  padding: 24px 8px 48px;
}
.hero-card,
.flow-card,
.content-card,
.summary-card,
.report-note,
.empty-preview {
  background: #fff;
  border: 1px solid #dfe3e8;
  border-radius: 12px;
  box-shadow: 0 4px 18px rgba(31, 45, 61, 0.06);
}
.hero-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 28px;
  padding: 24px 26px;
  border-left: 5px solid #321fdb;
}
.eyebrow {
  margin-bottom: 6px;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: .08em;
  color: #321fdb;
}
.hero-card h2 {
  margin: 0;
  color: #1f2d3d;
  font-size: 27px;
  font-weight: 900;
}
.hero-card p {
  max-width: 830px;
  margin: 9px 0 0;
  color: #667085;
  font-size: 14px;
  line-height: 1.75;
}
.primary-button {
  flex: 0 0 auto;
  border: 0;
  border-radius: 8px;
  background: #321fdb;
  color: #fff;
  padding: 11px 18px;
  font-weight: 800;
  cursor: pointer;
}
.flow-card {
  display: grid;
  grid-template-columns: 1fr 42px 1fr 42px 1fr 42px 1fr;
  align-items: center;
  margin-top: 16px;
  padding: 18px;
}
.flow-step {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 74px;
  border: 1px solid #e3e6ea;
  border-radius: 9px;
  padding: 12px;
}
.flow-step.active {
  border-color: #9dc2ff;
  background: #f4f8ff;
}
.flow-step.muted {
  opacity: .65;
}
.step-no {
  display: grid;
  place-items: center;
  width: 30px;
  height: 30px;
  flex: 0 0 30px;
  border-radius: 50%;
  background: #321fdb;
  color: #fff;
  font-weight: 900;
}
.flow-step strong {
  display: block;
  color: #1f2d3d;
  font-size: 14px;
}
.flow-step span {
  display: block;
  margin-top: 4px;
  color: #667085;
  font-size: 11px;
  line-height: 1.4;
}
.flow-arrow {
  text-align: center;
  color: #98a2b3;
  font-size: 24px;
  font-weight: 900;
}
.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  margin-top: 16px;
}
.summary-card {
  padding: 17px 18px;
}
.summary-label {
  color: #667085;
  font-size: 12px;
}
.summary-value {
  margin-top: 5px;
  color: #1f2d3d;
  font-size: 29px;
  font-weight: 900;
}
.summary-value.small {
  font-size: 19px;
  padding-top: 8px;
}
.content-card {
  margin-top: 16px;
  overflow: hidden;
}
.card-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  padding: 17px 20px;
  background: #f8f9fb;
  border-bottom: 1px solid #e4e7ec;
}
.card-heading h3 {
  margin: 2px 0 0;
  color: #1f2d3d;
  font-size: 18px;
  font-weight: 900;
}
.card-heading p {
  margin: 0;
  color: #667085;
  font-size: 12px;
}
.section-index {
  color: #321fdb;
  font-size: 10px;
  font-weight: 900;
  letter-spacing: .1em;
}
.major-grid,
.group-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(235px, 1fr));
  gap: 12px;
  padding: 18px;
}
.major-card,
.group-card {
  border: 1px solid #dfe3e8;
  border-radius: 9px;
  padding: 14px;
  background: #fff;
}
.major-card.ready {
  border-left: 4px solid #198754;
}
.major-card.pending {
  border-left: 4px solid #f0ad00;
}
.major-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}
.source-badge {
  border-radius: 999px;
  padding: 3px 8px;
  background: #ebe8ff;
  color: #321fdb;
  font-size: 11px;
  font-weight: 900;
}
.state-badge {
  display: inline-block;
  border-radius: 999px;
  padding: 4px 8px;
  font-size: 11px;
  font-weight: 800;
  white-space: nowrap;
}
.state-badge.green {
  background: #d1e7dd;
  color: #0f5132;
}
.state-badge.yellow {
  background: #fff3cd;
  color: #664d03;
}
.major-name {
  margin-top: 12px;
  color: #1f2d3d;
  font-size: 17px;
  font-weight: 900;
}
.major-count {
  margin-top: 4px;
  color: #667085;
  font-size: 12px;
}
.tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 12px;
}
.detail-tag {
  border-radius: 999px;
  padding: 5px 9px;
  background: #e7f1ff;
  color: #084298;
  font-size: 12px;
  font-weight: 800;
}
.pending-note {
  margin-top: 12px;
  border-radius: 6px;
  padding: 8px 9px;
  background: #fff9e8;
  color: #7a5900;
  font-size: 11px;
  line-height: 1.5;
}
.group-path {
  display: flex;
  align-items: center;
  gap: 7px;
  color: #667085;
  font-size: 12px;
}
.group-path strong {
  color: #1f2d3d;
  font-size: 17px;
}
.path-arrow {
  color: #98a2b3;
}
.group-meta {
  margin-top: 10px;
  color: #667085;
  font-size: 12px;
}
.group-total {
  margin-top: 9px;
  color: #0f5132;
  font-size: 14px;
  font-weight: 900;
}
.group-warning {
  margin-top: 9px;
  border-radius: 6px;
  padding: 7px 9px;
  background: #fff3cd;
  color: #664d03;
  font-size: 12px;
}
.table-wrap {
  overflow-x: auto;
  margin: 18px;
  border: 1px solid #dfe3e8;
  border-radius: 9px;
}
table {
  width: 100%;
  min-width: 1040px;
  border-collapse: collapse;
  font-size: 12px;
}
th {
  padding: 10px 9px;
  background: #4f5459;
  color: #fff;
  text-align: center;
  font-weight: 800;
}
td {
  padding: 10px 9px;
  border-bottom: 1px solid #edf0f2;
  text-align: center;
  vertical-align: middle;
}
.strong-cell {
  color: #1f2d3d;
  font-weight: 800;
}
.left-cell {
  text-align: left;
}
.item-name {
  color: #1f2d3d;
  font-weight: 800;
}
.item-note {
  margin-top: 3px;
  color: #98a2b3;
  font-size: 10px;
}
.number-cell {
  text-align: right;
  font-variant-numeric: tabular-nums;
}
.report-note {
  margin-top: 16px;
  padding: 16px 18px;
  border-left: 5px solid #198754;
  color: #344054;
  font-size: 13px;
  line-height: 1.75;
}
.report-note strong {
  color: #0f5132;
}
.report-note span {
  display: block;
  color: #667085;
}
.empty-preview {
  margin-top: 16px;
  padding: 38px;
  text-align: center;
  color: #98a2b3;
  border-style: dashed;
  box-shadow: none;
}
@media (max-width: 900px) {
  .hero-card {
    align-items: stretch;
    flex-direction: column;
  }
  .flow-card {
    grid-template-columns: 1fr;
    gap: 8px;
  }
  .flow-arrow {
    transform: rotate(90deg);
  }
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .card-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
