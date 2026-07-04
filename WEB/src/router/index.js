/*
 * Vue Router 4 factory (WO-94 Step 7).
 *
 * Assembles routes from each feature module (modules/<name>/routes) and applies
 * a 4-step navigation guard:
 *   (1) requiresAuth + no token          -> /auth/login (with ?redirect)
 *   (2) authenticated on an /auth/* route -> /dashboard
 *   (3) mustChangePassword               -> /change-password
 *   (4) deepest meta.permissions unmet    -> notify + redirect "/"
 *
 * (423 setupRequired -> /setup is handled by the axios response interceptor.)
 */
import { createRouter, createWebHistory, createWebHashHistory } from 'vue-router'
import { useAuthStore } from 'stores/auth'
import { notifyNegative } from 'composables/useNotify'
import { appShellRoute, notAuthorizedRoute, catchAllRoute } from './routes'

// Feature modules
import authRoutes from 'modules/auth/routes'
import dashboardRoutes from 'modules/dashboard/routes'
import widgetRoutes from 'modules/widget/routes'
import storesRoutes from 'modules/stores/routes'
import currenciesRoutes from 'modules/currencies/routes'
import emailTemplatesRoutes from 'modules/email-templates/routes'
import brandingRoutes from 'modules/branding/routes'
import settingsRoutes from 'modules/settings/routes'
import credentialsRoutes from 'modules/credentials/routes'
import usersRoutes from 'modules/users/routes'
import rolesRoutes from 'modules/roles/routes'
import catalogRoutes from 'modules/catalog/routes'
import ordersRoutes from 'modules/orders/routes'
import pricingRoutes from 'modules/pricing/routes'
import shippingRoutes from 'modules/shipping/routes'
import reportsRoutes from 'modules/reports/routes'
import taxRoutes from 'modules/tax/routes'
import customersRoutes from 'modules/customers/routes'
import storefrontRoutes from 'modules/storefront/routes'
import storageRoutes from 'modules/storage/routes'
import emailAccountsRoutes from 'modules/email-accounts/routes'
import webhooksRoutes from 'modules/webhooks/routes'

// Append every app-module child route under the authenticated shell.
const moduleChildren = [
  ...dashboardRoutes,
  ...widgetRoutes,
  ...storesRoutes,
  ...currenciesRoutes,
  ...emailTemplatesRoutes,
  ...brandingRoutes,
  ...settingsRoutes,
  ...credentialsRoutes,
  ...usersRoutes,
  ...rolesRoutes,
  ...catalogRoutes,
  ...ordersRoutes,
  ...pricingRoutes,
  ...shippingRoutes,
  ...reportsRoutes,
  ...taxRoutes,
  ...customersRoutes,
  ...storageRoutes,
  ...emailAccountsRoutes,
  ...webhooksRoutes
]
appShellRoute.children.push(...moduleChildren)

const routes = [
  ...authRoutes, // /auth/*, /setup, /change-password (own layout)
  appShellRoute,
  ...storefrontRoutes, // /shop/* public customer storefront (own layout, no auth)
  notAuthorizedRoute,
  catchAllRoute
]

function deepestPermissions (to) {
  for (let i = to.matched.length - 1; i >= 0; i--) {
    const perms = to.matched[i].meta && to.matched[i].meta.permissions
    if (perms && perms.length) return perms
  }
  return null
}

export default function () {
  const createHistory =
    process.env.VUE_ROUTER_MODE === 'history' ? createWebHistory : createWebHashHistory

  const Router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    routes,
    history: createHistory(process.env.VUE_ROUTER_BASE)
  })

  Router.beforeEach((to) => {
    const auth = useAuthStore()
    const isAuthed = auth.isAuthenticated
    const requiresAuth = to.matched.some((r) => r.meta && r.meta.requiresAuth)
    const isAuthArea = to.path.startsWith('/auth')

    // (1) Protected route without a session.
    if (requiresAuth && !isAuthed) {
      const redirect = to.fullPath && to.fullPath !== '/' ? to.fullPath : undefined
      return { path: '/auth/login', query: redirect ? { redirect } : undefined }
    }

    // (2) Already signed in but visiting the auth area — send each role to its home
    // (WO-112 unified login: staff → dashboard, customers (no role) → storefront).
    if (isAuthed && isAuthArea) {
      return { path: auth.roles.length ? '/dashboard' : '/shop' }
    }

    // (3) Forced password change.
    if (
      isAuthed &&
      auth.mustChangePassword &&
      to.name !== 'change-password' &&
      to.name !== 'setup'
    ) {
      return { path: '/change-password' }
    }

    // (4) Permission gate on the deepest matched record.
    const requiredPerms = deepestPermissions(to)
    if (requiredPerms && !auth.hasAnyPermission(requiredPerms)) {
      notifyNegative('You are not authorized to view this page.')
      return { path: '/' }
    }

    return true
  })

  return Router
}
