const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()
export const apiBaseUrl = (configuredBaseUrl || '/api').replace(/\/$/, '')

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message)
  }
}

export function queryString(values: Record<string, unknown>): string {
  const params = new URLSearchParams()

  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      params.set(key, String(value))
    }
  })

  const query = params.toString()
  return query ? `?${query}` : ''
}

export async function api<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)

  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
  })

  const contentType = response.headers.get('content-type') ?? ''
  const payload = contentType.includes('application/json')
    ? await response.json()
    : await response.text()

  if (!response.ok) {
    const message =
      typeof payload === 'object' && payload && 'message' in payload
        ? String(payload.message)
        : `請求失敗（HTTP ${response.status}）`
    throw new ApiError(message, response.status)
  }

  return payload as T
}

export async function apiForm<T>(path: string, form: FormData): Promise<T> {
  return api<T>(path, { method: 'POST', body: form })
}
