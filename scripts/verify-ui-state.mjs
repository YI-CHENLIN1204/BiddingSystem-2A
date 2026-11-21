#!/usr/bin/env node
/**
 * 2A graduation safe state verification.
 * Read-only: checks API saved cases/versions/runs against a precisely
 * identified localhost Docker PostgreSQL 55433 synthetic container.
 *
 * Usage: node scripts/verify-ui-state.mjs
 * Requires Docker Desktop and the existing 5263 backend; no secrets output.
 * Does NOT create cases, run new analyses, publish history or touch volumes.
 */
import { execFileSync } from 'node:child_process'
import { createHash } from 'node:crypto'

const BASE = 'http://127.0.0.1:5263/api'
const PROJECT = 'graduation-src-synthetic'
const SERVICE = 'graduation-src-postgres'
const DB = 'bidding2a_graduation_src'
let pass = 0

function check(condition, reason) {
  if (!condition) throw new Error(reason)
}
function success(message) {
  console.log('[PASS] ' + message)
  pass++
}
function docker(args) {
  return execFileSync('docker', args, {
    encoding: 'utf8',
    timeout: 20000,
    maxBuffer: 4 * 1024 * 1024,
    windowsHide: true,
  }).trim()
}
async function get(path) {
  const response = await fetch(BASE + path, { signal: AbortSignal.timeout(30000) })
  check(response.ok, path + ': HTTP ' + response.status)
  check((response.headers.get('content-type') || '').includes('application/json'),
    path + ': response is not JSON')
  return response.json()
}
function psql(id, sql) {
  return docker(['exec', id, 'psql', '-U', 'graduation', '-d', DB,
    '-X', '-A', '-t', '-v', 'ON_ERROR_STOP=1', '-c', sql])
}
async function run() {
  check(process.argv.length === 2, 'Usage: node scripts/verify-ui-state.mjs')
  const containers = docker([
    'ps', '--filter', 'label=com.docker.compose.project=' + PROJECT,
    '--filter', 'label=com.docker.compose.service=' + SERVICE,
    '--format', '{{.ID}}',
  ]).split(/\r?\n/).filter(Boolean)
  check(containers.length === 1, 'Expected exactly one running graduation-src DB container')
  const id = containers[0]
  const inspected = JSON.parse(docker(['inspect', id]))
  check(Array.isArray(inspected) && inspected.length === 1, 'Unexpected docker inspect result')
  const container = inspected[0]
  check(container.Config?.Labels?.['com.docker.compose.project'] === PROJECT,
    'Docker compose project mismatch')
  check(container.Config?.Labels?.['com.docker.compose.service'] === SERVICE,
    'Docker compose service mismatch')
  const bindings = container.HostConfig?.PortBindings?.['5432/tcp'] || []
  check(bindings.length === 1 && bindings[0].HostIp === '127.0.0.1' &&
    bindings[0].HostPort === '55433', 'Database must bind only to 127.0.0.1:55433')
  const env = container.Config?.Env || []
  check(env.includes('POSTGRES_DB=' + DB) && env.includes('POSTGRES_USER=graduation'),
    'Synthetic database name or user mismatch')
  success('Docker SRC isolated database identity / localhost binding')

  const realName = psql(id, 'SELECT current_database();')
  check(realName === DB, 'psql connected to unexpected database')
  const rawRows = psql(id,
    "SELECT analysis_case_id::text || '|' || md5(case_name) " +
    'FROM analysis_case ORDER BY analysis_case_id;')
  const dbRows = rawRows ? rawRows.split(/\r?\n/) : []
  const ids = new Map()
  for (const row of dbRows) {
    const [idText, digest] = row.split('|')
    const numericId = Number(idText)
    check(Number.isSafeInteger(numericId) && numericId > 0 &&
      /^[0-9a-f]{32}$/.test(digest || '') && !ids.has(numericId),
      'Invalid or duplicate DB case signature')
    ids.set(numericId, digest)
  }
  check(ids.size > 0, 'No existing DB cases; cannot attest API identity')

  const cases = await get('/analysis-cases')
  check(Array.isArray(cases) && cases.length === ids.size,
    'API case count does not match isolated 55433 DB')
  const seen = new Set()
  for (const c of cases) {
    const id = Number(c.analysisCaseId)
    check(Number.isSafeInteger(id) && !seen.has(id), 'Invalid or duplicate API case ID')
    seen.add(id)
    check(typeof c.caseName === 'string', 'API caseName is missing')
    const digest = createHash('md5').update(c.caseName, 'utf8').digest('hex')
    check(ids.get(id) === digest, 'API case ID/name signature differs from 55433 DB')
  }
  success('5263 API matches all ' + cases.length + ' isolated DB case ID/name signatures')

  const approvedText = psql(id,
    "SELECT count(*) FROM project_master " +
    "WHERE history_status = 'Approved' AND is_deleted = false;")
  const approvedCount = Number(approvedText)
  check(Number.isSafeInteger(approvedCount) && approvedCount >= 5,
    'Invalid isolated DB approved project count')
  const readiness = await get('/major-category-price/readiness')
  check(readiness?.status === 'ReadyForEstimation' &&
    Number(readiness.eligibleMetadataProjectCount) === approvedCount,
    'API price-readiness project count differs from isolated DB')
  success('Approved historical project cohort count agrees: ' + approvedCount)

  let savedRunCount = 0
  let verifiedRuns = 0
  let totalVersions = 0
  for (const c of cases) {
    const caseId = Number(c.analysisCaseId)
    const [detail, runs] = await Promise.all([
      get('/analysis-cases/' + caseId),
      get('/analysis-cases/' + caseId + '/runs'),
    ])
    check(detail?.case?.analysisCaseId === caseId &&
      Array.isArray(detail.versions) && detail.versions.length > 0,
      'Case detail/version missing: ' + caseId)
    check(Array.isArray(runs), 'Run summary not an array: ' + caseId)
    const versions = new Set(detail.versions.map(x => x.analysisBidVersionId))
    check(versions.size === detail.versions.length, 'Duplicate version ID: ' + caseId)
    totalVersions += versions.size
    savedRunCount += runs.length
    for (const run of runs) {
      check(versions.has(run.analysisBidVersionId),
        'Run references wrong case/version: ' + caseId)
    }
    if (runs.length > 0) {
      const first = runs[0]
      const saved = await get('/analysis-cases/' + caseId + '/runs/' + first.analysisRunId)
      check(saved?.analysisRunId === first.analysisRunId &&
        saved?.analysisBidVersionId === first.analysisBidVersionId &&
        saved?.runType === first.runType &&
        saved.result !== undefined && saved.result !== null,
        'Saved run cannot be retrieved intact: ' + caseId)
      verifiedRuns++
    }
  }
  check(savedRunCount > 0 && verifiedRuns > 0,
    'No saved run exists to verify persistence')
  success('Saved API cases / versions / runs: ' + cases.length +
    ' cases, ' + totalVersions + ' versions, ' + savedRunCount + ' runs; ' +
    verifiedRuns + ' retrieved run payloads')

  console.log('[RESULT] ' + pass + '/4 isolated read-only checks PASS')
  console.log('[NOTE] API/DB case signatures strongly corroborate 5263 -> 55433, but not a formal connection-string attestation')
  console.log('[NOT VERIFIED] Browser click/upload, new analysis execution, history publish, audit-log and accuracy')
}
run().catch(error => {
  console.error('[FAIL] ' + (error instanceof Error ? error.message : String(error)))
  process.exitCode = 1
})
