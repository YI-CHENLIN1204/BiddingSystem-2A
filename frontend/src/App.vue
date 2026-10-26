<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const menuOpen = ref(false)
const sidebarCollapsed = ref(false)

function toggleNavigation() {
  if (window.matchMedia('(max-width: 960px)').matches) {
    menuOpen.value = !menuOpen.value
  } else {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }
}
const pageTitle = computed(() => String(route.meta.title ?? '系統'))
const sectionTitle = computed(() => route.name === 'audit-logs' ? '系統' : '工程類')

const navigation = [
  { to: '/analysis-cases', label: '待偵錯案件', section: '工程類' },
  { to: '/', label: '系統總覽', section: '工程類' },
  { to: '/settings', label: '參數管理', section: '工程類' },
  { to: '/estimation', label: '結構體工程估算', section: '工程類' },
  { to: '/final-bid', label: '決標回寫', section: '工程類' },
  { to: '/projects', label: '建案管理', section: '工程類' },
  { to: '/audit-logs', label: '操作紀錄', section: '系統' },
]

const engineeringNavigation = navigation.filter((item) => item.section === '工程類')
const systemNavigation = navigation.filter((item) => item.section === '系統')
</script>

<template>
  <div class="app-shell" :class="{ 'sidebar-collapsed': sidebarCollapsed }">
    <aside id="main-navigation" class="sidebar" :class="{ open: menuOpen }">
      <div class="sidebar-brand"><span class="brand-title">崴正營造</span></div>
      <div class="brand-user">估算系統</div>

      <nav class="sidebar-section">
        <div class="sidebar-heading">工程類</div>
        <RouterLink
          v-for="item in engineeringNavigation"
          :key="item.to"
          :to="item.to"
          class="sidebar-link"
          @click="menuOpen = false"
        >
          <span class="sidebar-icon"></span>
          <span>{{ item.label }}</span>
        </RouterLink>

        <div class="sidebar-heading">系統</div>
        <RouterLink
          v-for="item in systemNavigation"
          :key="item.to"
          :to="item.to"
          class="sidebar-link"
          @click="menuOpen = false"
        >
          <span class="sidebar-icon"></span>
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>
    </aside>

    <section class="main-wrapper">
      <header class="topbar">
        <button
          class="topbar-menu"
          type="button"
          aria-label="展開或收合主選單"
          aria-controls="main-navigation"
          :aria-expanded="menuOpen || !sidebarCollapsed"
          @click="toggleNavigation"
        >≡</button>
        <div class="topbar-user" aria-hidden="true"></div>
      </header>

      <div class="breadcrumb-strip">{{ sectionTitle }} / {{ pageTitle }}</div>
      <div class="main-shell"><main><RouterView /></main></div>
    </section>
  </div>
</template>
