#!/usr/bin/env node
/**
 * Graduation UI/API read-only preflight.
 * Node 20+; no additional packages; no DB writes.
 * Usage:
 *   node scripts/verify-ui-preflight.mjs
 *   node scripts/verify-ui-preflight.mjs --excel .\\graduation-test.xlsx
 *
 * This is NOT a browser interaction test, a database identity attestation,
 * or permission to publish any history. Fail closed on missing evidence.
 */
import { readFile } from 'node:fs/promises'
import { basename, resolve } from 'node:path'

const apiBase = 'http://127.0.0.1:5263'
const frontendBase = 'http://127.0.0.1:5174'
let passed = 0

function requireTrue(condition, reason) {
  if (!condition) throw new Error(reason)
}
function ok(label, detail = '') {
  passed++
  console.log('[PASS] ' + label + (detail ? ': ' + detail : ''))
}
async function requestJson(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    signal: AbortSignal.timeout(45000),
  })
  const contentType = response.headers.get('content-type') ?? ''
  requireTrue(response.ok, url + ' returned HTTP ' + response.status)
  requireTrue(contentType.includes('application/json'),
    url + ' returned non-JSON content (possible wrong frontend proxy)')
  return response.json()
}
async function get(path) {
  return requestJson(apiBase + '/api' + path)
}
async function run() {
  const args = process.argv.slice(2)
  requireTrue(args.length === 0 || (args.length === 2 && args[0] === '--excel'),
    'Usage: node scripts/verify-ui-preflight.mjs [--excel PATH]')
  const excel = args.length ? resolve(args[1]) : null

  const health = await get('/system/health')
  requireTrue(health?.status === 'ok', '5263 backend health is not ok')
  ok('Backend 5263 health')

  const policy = await get('/analysis-cases/policy')
  requireTrue(policy?.selfUpload?.enabled === true, '2A self-upload is not enabled')
  requireTrue(policy?.sourcePolicy?.standaloneFirst === true, '2A standalone-first policy missing')
  const categories = policy?.quantityEstimation?.detectionCategories
  requireTrue(Array.isArray(categories) && categories.includes('鋼筋') && categories.includes('混凝土'),
    'Core quantity scopes missing from API policy')
  ok('Standalone upload policy and core quantity scopes')

  const cases = await get('/analysis-cases')
  requireTrue(Array.isArray(cases), 'GET analysis-cases did not return an array')
  ok('Case list API', String(cases.length) + ' saved cases (not an E2E upload check)')

  const price = await get('/major-category-price/readiness')
  requireTrue(price?.status === 'ReadyForEstimation' &&
    Number(price?.availableProjectCount) >= Number(price?.minimumSampleCount) &&
    Number(price?.minimumSampleCount) > 0, 'Price readiness is not ready or sample counts invalid')
  ok('Major-category price readiness',
    String(price.availableProjectCount) + ' available projects')

  const quantity = await get('/detection-category/readiness')
  requireTrue(Array.isArray(quantity?.items) && quantity.items.length > 0,
    'Detection-category readiness items missing')
  const names = quantity.items.map(item => item.detectionCategoryName)
  requireTrue(names.includes('鋼筋') && names.includes('混凝土'),
    'Rebar/concrete readiness missing')
  const needed = quantity.items.filter(item =>
    item.detectionCategoryName === '鋼筋' || item.detectionCategoryName === '混凝土')
  requireTrue(needed.every(item => Number(item.historicalProjectCount) >= Number(item.minimumSampleCount) &&
      Number(item.minimumSampleCount) > 0),
    'Rebar/concrete historical samples insufficient')
  ok('Rebar and concrete readiness', needed.length + ' supported categories')

  const routedHealth = await requestJson(frontendBase + '/api/system/health')
  requireTrue(routedHealth?.status === 'ok', '5174 /api reverse proxy health failed')
  ok('Frontend 5174 /api proxy responds')

  const page = await fetch(frontendBase + '/analysis-cases', {
    signal: AbortSignal.timeout(15000),
  })
  requireTrue(page.ok, 'Frontend analysis-cases route returned HTTP ' + page.status)
  requireTrue((page.headers.get('content-type') ?? '').includes('text/html'),
    'Frontend analysis-cases route is not HTML')
  const html = await page.text()
  requireTrue(html.includes('id="app"'), 'Frontend route does not contain Vue app root')
  ok('Vue analysis-cases route serves app HTML')

  if (excel) {
    const bytes = await readFile(excel)
    const filename = basename(excel)
    requireTrue(bytes.length > 0 && bytes.length <= 100 * 1024 * 1024,
      'Excel must be between 1 byte and 100 MiB')
    const upload = new FormData()
    upload.append('excelFile', new Blob([bytes]), filename)
    const preview = await requestJson(apiBase + '/api/tender-import/preview', {
      method: 'POST',
      body: upload,
    })
    requireTrue(Array.isArray(preview?.sheets) && preview.sheets.length > 0,
      'Excel preview returned zero worksheets')
    const selected = preview.sheets
      .filter(sheet => Number(sheet.itemCount) > 0 && !sheet.name.includes('總表'))
      .map(sheet => sheet.name)
    requireTrue(selected.length > 0, 'No identifiable detail sheet; manual parser review needed')
    ok('Excel preview', preview.sheets.length + ' sheets, ' +
      selected.length + ' identified detail sheets')
    const inspection = new FormData()
    inspection.append('excelFile', new Blob([bytes]), filename)
    inspection.append('includedSheetNames', JSON.stringify(selected))
    const report = await requestJson(apiBase + '/api/tender-import/inspect', {
      method: 'POST',
      body: inspection,
    })
    requireTrue(Number(report?.totalRows) > 0, 'Excel inspection has zero candidate rows')
    ok('Excel inspection', report.totalRows + ' candidate rows, ' +
      report.warningRows + ' warning rows')
  } else {
    console.log('[NOT RUN] Excel preview/inspect: supply --excel with a new synthetic test workbook')
  }
  console.log('[RESULT] ' + passed + ' read-only checks PASS')
  console.log('[NOT VERIFIED] Browser interactions, new case upload, analysis Run persistence, final-bid approval/publish, audit log')
  console.log('[NOT VERIFIED] 5263 DB identity: confirm 55433/bidding2a_graduation_src independently before any write')
}

run().catch(error => {
  console.error('[FAIL] ' + (error instanceof Error ? error.message : String(error)))
  process.exitCode = 1
})
