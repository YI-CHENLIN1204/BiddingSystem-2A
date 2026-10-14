<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import ProjectFields from '../components/ProjectFields.vue'
import { useSystemOptions } from '../composables/useSystemOptions'
import { api, queryString } from '../services/api'
import type { ProjectForm, ProjectSummary } from '../types'

const projects = ref<ProjectSummary[]>([])
const search = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const message = ref('')
const editingId = ref<number | null>(null)
const showEditor = ref(false)
const { options, error: optionsError } = useSystemOptions()
const form = ref<ProjectForm>(emptyForm())
let searchTimer: number | undefined

function emptyForm(): ProjectForm {
  return {
    projectName: '', projectType: '', structureType: '', city: '',
    totalFloorArea: null, siteArea: null, floorCount: null, basementFloorCount: 0, projectMonth: '',
  }
}

function formatMonth(value?: string) {
  return value ? value.slice(0, 7) : '未設定'
}

function openEditor(project: ProjectSummary) {
  if (project.historyStatus === 'Approved') {
    error.value = '此建案已納入 2A 歷史基準；如需修改基本資料，請先進入建案並重新開啟審核。'
    return
  }
  editingId.value = project.projectId
  form.value = {
    projectName: project.projectName,
    projectType: project.projectType ?? '',
    structureType: project.structureType ?? '',
    city: project.city ?? '',
    totalFloorArea: project.totalFloorArea ?? null,
    siteArea: project.siteArea ?? null,
    floorCount: project.floorCount ?? null,
    basementFloorCount: project.basementFloorCount ?? 0,
    projectMonth: project.startDate?.slice(0, 7) ?? '',
  }
  error.value = ''
  showEditor.value = true
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const response = await api<{ items: ProjectSummary[] }>(`/projects${queryString({ search: search.value.trim() })}`)
    projects.value = response.items
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '無法載入建案'
  } finally {
    loading.value = false
  }
}

async function save() {
  if (editingId.value == null) return
  saving.value = true
  error.value = ''
  try {
    await api(`/projects/${editingId.value}`, { method: 'PUT', body: JSON.stringify(form.value) })
    showEditor.value = false
    message.value = '建案資料已更新。'
    await load()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '建案儲存失敗'
  } finally {
    saving.value = false
  }
}

async function remove(project: ProjectSummary) {
  if (!window.confirm(`確定要刪除建案「${project.projectName}」嗎？\n\n建案會被標記為已刪除，相關歷史交易也會停用。`)) return
  error.value = ''
  try {
    await api(`/projects/${project.projectId}`, { method: 'DELETE' })
    message.value = `已刪除建案「${project.projectName}」。`
    await load()
  } catch (caught) {
    error.value = caught instanceof Error ? caught.message : '刪除失敗'
  }
}

watch(search, () => {
  window.clearTimeout(searchTimer)
  searchTimer = window.setTimeout(load, 300)
})

onMounted(load)
</script>

<template>
  <div>
    <div class="mb-3">
      <h3 class="mb-1">建案管理</h3>
      <div class="text-muted">查詢與管理歷史決標建案</div>
    </div>

    <div v-if="optionsError || error" class="alert alert-danger">{{ optionsError || error }}</div>
    <div v-if="message" class="alert alert-success alert-dismissible">
      {{ message }}
      <button type="button" class="btn-close" aria-label="關閉" @click="message = ''"></button>
    </div>

    <div class="card p-3 mb-3">
      <input v-model="search" class="form-control" placeholder="搜尋建案名稱或縣市" aria-label="搜尋建案" />
    </div>

    <div v-if="loading" class="card p-4 text-muted">載入建案資料中...</div>
    <div v-else-if="!projects.length" class="card p-4 text-muted">目前沒有符合條件的建案</div>

    <article v-for="project in projects" v-else :key="project.projectId" class="card p-3 mb-3 project-card">
      <div class="d-flex justify-content-between align-items-start gap-3">
        <div>
          <div class="d-flex align-items-center gap-2 mb-2 flex-wrap">
            <h5 class="mb-0">{{ project.projectName }}</h5>
            <span class="badge" :class="project.hasFinalBid ? 'bg-secondary' : 'bg-light text-dark border'">
              {{ project.hasFinalBid ? '已有決標資料' : '尚無決標資料' }}
            </span>
            <span
              v-if="project.hasFinalBid"
              class="badge"
              :class="project.historyStatus === 'Approved' ? 'bg-success' : 'bg-warning text-dark'"
            >
              {{ project.historyStatus === 'Approved' ? '已納入 2A 模型' : '待歷史基準審核' }}
            </span>
          </div>
          <div class="text-muted small">
            {{ project.projectType || '未設定' }} ・ {{ project.structureType || '未設定' }} ・
            {{ project.city || '未設定' }} ・ {{ formatMonth(project.startDate) }}
          </div>
        </div>
        <div class="dropdown project-actions">
          <button class="btn btn-sm btn-light border" type="button" data-bs-toggle="dropdown" aria-expanded="false" aria-label="建案操作">⋯</button>
          <ul class="dropdown-menu dropdown-menu-end">
            <li><RouterLink class="dropdown-item" :to="`/projects/${project.projectId}`">查看建案</RouterLink></li>
            <li>
              <button
                class="dropdown-item"
                type="button"
                :disabled="project.historyStatus === 'Approved'"
                @click="openEditor(project)"
              >
                {{ project.historyStatus === 'Approved' ? '基本資料已鎖定' : '編輯基本資料' }}
              </button>
            </li>
            <li><hr class="dropdown-divider" /></li>
            <li><button class="dropdown-item text-danger" type="button" @click="remove(project)">刪除建案</button></li>
          </ul>
        </div>
      </div>

      <hr />
      <div class="row g-3 small">
        <div class="col-md-3"><div class="text-muted">總樓地板面積</div><strong>{{ project.totalFloorArea ?? '未設定' }} m²</strong></div>
        <div class="col-md-3"><div class="text-muted">基地面積</div><strong>{{ project.siteArea ?? '未設定' }} m²</strong></div>
        <div class="col-md-3"><div class="text-muted">地上 / 地下樓層</div><strong>{{ project.floorCount ?? '-' }} / {{ project.basementFloorCount ?? '-' }}</strong></div>
        <div class="col-md-3"><div class="text-muted">決標工項</div><strong>{{ project.finalBidItemCount ?? 0 }}</strong></div>
        <div class="col-md-3"><div class="text-muted">有效歷史交易</div><strong>{{ project.historicalTransactionCount ?? 0 }}</strong></div>
        <div class="col-md-3"><div class="text-muted">歷史基準狀態</div><strong>{{ project.historyStatus === 'Approved' ? '已發布' : '審核中' }}</strong></div>
      </div>
    </article>

    <div v-if="showEditor" class="modal-backdrop-custom" role="presentation" @mousedown.self="showEditor = false">
      <div class="modal-dialog modal-lg" role="dialog" aria-modal="true" aria-labelledby="project-editor-title">
        <form class="modal-content" @submit.prevent="save">
          <div class="modal-header">
            <h5 id="project-editor-title" class="modal-title">編輯建案資料</h5>
            <button type="button" class="btn-close" aria-label="關閉" @click="showEditor = false"></button>
          </div>
          <div class="modal-body">
            <ProjectFields
              v-model="form"
              :options="options"
              include-name
              :validate="false"
              area-label="總樓地板面積（m²）"
              floor-label="地上樓層數"
              basement-floor-label="地下樓層數"
            />
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="showEditor = false">取消</button>
            <button type="submit" class="btn btn-primary" :disabled="saving">{{ saving ? '儲存中...' : '儲存' }}</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.project-card { overflow: visible; }
.project-actions { position: relative; }
.modal-backdrop-custom {
  position: fixed; inset: 0; z-index: 1080; display: grid; place-items: center;
  padding: 1.5rem; overflow-y: auto; background: rgba(0, 0, 0, .5);
}
.modal-backdrop-custom .modal-dialog { width: min(800px, 100%); margin: auto; }
.modal-content { max-height: calc(100vh - 3rem); overflow-y: auto; }
.modal-backdrop-custom :deep(.form-grid) { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.modal-backdrop-custom :deep(.form-grid .span-2:first-child) { grid-column: 1 / -1; }
.modal-backdrop-custom :deep(.field) { gap: 0; }
.modal-backdrop-custom :deep(.field > span) {
  margin-bottom: .5rem;
  color: inherit;
  font-size: 1rem;
  font-weight: 400;
}
.modal-backdrop-custom :deep(.field input),
.modal-backdrop-custom :deep(.field select) {
  min-height: 38px;
  padding: .375rem .75rem;
  border-color: var(--wz-border);
  border-radius: 6px;
  line-height: 1.5;
}
@media (max-width: 767px) {
  .modal-backdrop-custom :deep(.form-grid) { grid-template-columns: 1fr; }
  .modal-backdrop-custom :deep(.form-grid .span-2:first-child) { grid-column: auto; }
}
</style>
