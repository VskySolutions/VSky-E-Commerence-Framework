/*
 * Base route skeleton (WO-94 Step 7).
 *
 * - appShellRoute: the authenticated layout at "/", redirecting "" -> /dashboard.
 *   Feature-module child routes are appended in router/index.js.
 * - /not-authorized and the catch-all 404 are always present.
 */

export const appShellRoute = {
  path: '/',
  component: () => import('layouts/layout.vue'),
  meta: { requiresAuth: true },
  children: [{ path: '', redirect: '/dashboard' }]
}

export const notAuthorizedRoute = {
  path: '/not-authorized',
  name: 'not-authorized',
  meta: { title: 'Not Authorized' },
  component: () => import('shared/error_not_authorized.vue')
}

export const catchAllRoute = {
  path: '/:catchAll(.*)*',
  name: 'not-found',
  meta: { title: 'Not Found' },
  component: () => import('shared/error_not_found.vue')
}

export default [appShellRoute, notAuthorizedRoute, catchAllRoute]
