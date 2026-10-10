import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '../views/DashboardView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardView, meta: { title: '標單異常偵測' } },
    { path: '/analysis-cases', name: 'analysis-cases', component: () => import('../views/AnalysisCasesView.vue'), meta: { title: '待偵錯案件' } },
    { path: '/analysis-cases/:id', name: 'analysis-case-detail', component: () => import('../views/AnalysisCaseDetailView.vue'), meta: { title: '待偵錯案件詳細資料' } },
    { path: '/estimation', name: 'estimation', component: () => import('../views/EstimationView.vue'), meta: { title: '工程用量估算' } },
    { path: '/final-bid', name: 'final-bid', component: () => import('../views/FinalBidView.vue'), meta: { title: '決標資料回寫' } },
    { path: '/projects', name: 'projects', component: () => import('../views/ProjectsView.vue'), meta: { title: '建案管理' } },
    { path: '/projects/:id', name: 'project-detail', component: () => import('../views/ProjectDetailView.vue'), meta: { title: '建案詳細資料' } },
    { path: '/settings', name: 'settings', component: () => import('../views/SettingsView.vue'), meta: { title: '參數管理' } },
    { path: '/audit-logs', name: 'audit-logs', component: () => import('../views/AuditLogView.vue'), meta: { title: '操作紀錄' } },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

router.afterEach((to) => {
  document.title = `${String(to.meta.title ?? '系統')} - 崴正工程估價系統`
})

export default router
