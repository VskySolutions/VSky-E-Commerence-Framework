/*
 * Response interceptor boot file (WO-94 Step 4).
 *
 * Attaches the RESPONSE interceptor to the authenticated `http` instance:
 *   - 423 { setupRequired: true }  -> route to /setup.
 *   - 401 (non auth-endpoint, has refresh token) -> single-flight silent
 *     refresh, then retry the original request once. If refresh fails, wipe the
 *     session and redirect to /auth/login (preserving the attempted URL).
 *
 * Kept separate from boot/axios.js so the auth-store / router coupling lives in
 * exactly one place and axios.js stays importable anywhere.
 */
import { http } from 'src/boot/axios'
import { useAuthStore } from 'stores/auth'

function isAuthEndpoint (url = '') {
  return (
    url.includes('/api/auth/login') ||
    url.includes('/api/auth/refresh') ||
    url.includes('/api/auth/logout')
  )
}

export default ({ router }) => {
  // Ensures parallel 401s trigger only ONE refresh call.
  let refreshPromise = null

  http.interceptors.response.use(
    (response) => response,
    async (error) => {
      const original = error.config || {}
      const status = error.response ? error.response.status : null
      const data = error.response ? error.response.data : null

      // First-time setup gate.
      if (status === 423 && data && data.setupRequired) {
        if (router.currentRoute.value.path !== '/setup') {
          router.push('/setup')
        }
        return Promise.reject(error)
      }

      const auth = useAuthStore()

      const shouldRefresh =
        status === 401 &&
        !original._retry &&
        !isAuthEndpoint(original.url || '') &&
        !!auth.refreshToken

      if (shouldRefresh) {
        original._retry = true
        try {
          if (!refreshPromise) {
            refreshPromise = auth.refresh().finally(() => {
              refreshPromise = null
            })
          }
          await refreshPromise
          // Re-issue: the request interceptor re-reads the fresh token from
          // storage, so no manual header patching is required.
          return http(original)
        } catch (refreshError) {
          auth.clearSession()
          const redirect = router.currentRoute.value.fullPath
          router.push({
            path: '/auth/login',
            query: redirect && redirect !== '/' ? { redirect } : undefined
          })
          return Promise.reject(refreshError)
        }
      }

      return Promise.reject(error)
    }
  )
}
