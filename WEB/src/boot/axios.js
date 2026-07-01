/*
 * Axios boot file (WO-94 Step 4).
 *
 * Creates TWO axios instances:
 *   - `http`  : authenticated instance (baseURL from env). A request
 *               interceptor injects Authorization: Bearer <token>, a per-request
 *               X-Correlation-Id (uuid v4) and the X-Site-* headers read from the
 *               persisted user object in LocalStorage.
 *   - `http2` : anonymous instance (no auth header) for public endpoints
 *               (login, setup status, first-time setup, ...).
 *
 * The RESPONSE interceptor (401 refresh, redirects) lives in boot/interceptors.js
 * so this file stays free of store/router coupling and can be imported anywhere.
 */
import axios from 'axios'
import { getStoredToken, getStoredUser } from 'src/services/storage'

const API_BASE_URL = process.env.API_BASE_URL || ''

// ---- Authenticated instance -------------------------------------------------
const http = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000
})

// ---- Anonymous instance -----------------------------------------------------
const http2 = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000
})

// RFC-4122 v4 uuid; prefers the native crypto implementation.
function uuidV4 () {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

function browserTimeZone () {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
  } catch (e) {
    return 'UTC'
  }
}

// ---- Request interceptor (auth instance only) -------------------------------
http.interceptors.request.use((config) => {
  config.headers = config.headers || {}

  const token = getStoredToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  // Per-request correlation id for server-side tracing.
  config.headers['X-Correlation-Id'] = uuidV4()

  // Site/tenant context, sourced from the persisted user object.
  const user = getStoredUser() || {}
  const siteId = user.siteId ?? user.tenantId ?? user.activeTenantId ?? null
  const siteName = user.siteName ?? user.tenantName ?? null
  const siteTz = user.siteTimezone ?? user.timezone ?? user.timeZone ?? browserTimeZone()

  if (siteId != null) config.headers['X-Site-Id'] = String(siteId)
  if (siteName != null) config.headers['X-Site-Name'] = String(siteName)
  if (siteTz != null) config.headers['X-Site-Timezone'] = String(siteTz)

  return config
})

export default ({ app }) => {
  // Make instances available as $api / $anonApi / $axios inside templates.
  app.config.globalProperties.$axios = axios
  app.config.globalProperties.$api = http
  app.config.globalProperties.$anonApi = http2
}

export { http, http2 }
