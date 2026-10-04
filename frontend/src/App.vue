<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const menuOpen = ref(false)
const pageTitle = computed(() => String(route.meta.title ?? '系統'))

const navigation = [
  { to: '/', label: '投標異常分析', icon: '◎' },
  { to: '/estimation', label: '數量區間估算', icon: '∿' },
  { to: '/final-bid', label: '決標資料匯入', icon: '⇧' },
  { to: '/projects', label: '專案管理', icon: '▦' },
  { to: '/settings', label: '參數管理', icon: '⚙' },
  { to: '/audit-logs', label: '操作紀錄', icon: '≡' },
]
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar" :class="{ open: menuOpen }">
      <div class="brand">
        <span class="brand-mark">W</span>
        <div>
          <strong>崴正工程</strong>
          <small>估價管理系統</small>
        </div>
      </div>

      <nav>
        <RouterLink
          v-for="item in navigation"
          :key="item.to"
          :to="item.to"
          class="nav-link"
          @click="menuOpen = false"
        >
          <span class="nav-icon">{{ item.icon }}</span>
          {{ item.label }}
        </RouterLink>
      </nav>

      <div class="sidebar-footer">
        <span class="status-dot"></span>
        前後端分離版本
      </div>
    </aside>

    <section class="workspace">
      <header class="topbar">
        <button class="menu-button" type="button" @click="menuOpen = !menuOpen">☰</button>
        <div>
          <p class="eyebrow">BIDDING INTELLIGENCE</p>
          <h1>{{ pageTitle }}</h1>
        </div>
        <div class="api-badge"><span></span> API</div>
      </header>

      <main class="page-container">
        <RouterView />
      </main>
    </section>
  </div>
</template>
