/*
 * Storefront module routes (WO-19).
 *
 * Default-exports an ARRAY with ONE parent route: the PUBLIC storefront shell at
 * "/shop" (its own storefront_layout, not the authenticated app shell). Children
 * are public — no meta.requiresAuth and no meta.permissions.
 *
 * The parent router (router/index.js) should spread this into the TOP-LEVEL
 * `routes` array (alongside authRoutes / appShellRoute), NOT into
 * appShellRoute.children, because it brings its own layout and must not require
 * authentication.
 */
import { getStoredToken } from 'services/storage'

// Route guard for the authenticated customer account area. WO-112 unified auth into
// a single session, so it reads the shared token and redirects to the unified login.
function requireCustomer (to, from, next) {
  if (getStoredToken()) next()
  else next({ path: '/auth/login', query: { redirect: to.fullPath } })
}

export default [
  {
    path: '/shop',
    component: () => import('layouts/storefront_layout.vue'),
    children: [
      {
        path: '',
        name: 'shop-home',
        meta: { title: 'Shop' },
        component: () => import('modules/storefront/pages/home.vue')
      },
      {
        path: 'category/:idOrSlug',
        name: 'shop-category',
        meta: { title: 'Category' },
        component: () => import('modules/storefront/pages/category.vue')
      },
      {
        path: 'product/:idOrSlug',
        name: 'shop-product',
        meta: { title: 'Product' },
        component: () => import('modules/storefront/pages/product.vue')
      },
      {
        path: 'search',
        name: 'shop-search',
        meta: { title: 'Search' },
        component: () => import('modules/storefront/pages/search.vue')
      },
      {
        path: 'compare',
        name: 'shop-compare',
        meta: { title: 'Compare' },
        component: () => import('modules/storefront/pages/compare.vue')
      },
      {
        path: 'cart',
        name: 'shop-cart',
        meta: { title: 'Cart' },
        component: () => import('modules/storefront/pages/cart.vue')
      },
      {
        path: 'wishlist',
        name: 'shop-wishlist',
        meta: { title: 'Wishlist' },
        component: () => import('modules/storefront/pages/wishlist.vue')
      },
      {
        path: 'checkout',
        name: 'shop-checkout',
        meta: { title: 'Checkout' },
        component: () => import('modules/storefront/pages/checkout.vue')
      },

      // ---- Customer auth (public) — WO-21 / unified login WO-112 ----------
      {
        // The single login lives at /auth/login (WO-112); /shop/login redirects there,
        // preserving any post-login return path.
        path: 'login',
        name: 'shop-login',
        redirect: (to) => ({ path: '/auth/login', query: to.query })
      },
      {
        path: 'register',
        name: 'shop-register',
        meta: { title: 'Create account' },
        component: () => import('modules/storefront/pages/account/register.vue')
      },
      {
        path: 'verify-email',
        name: 'shop-verify-email',
        meta: { title: 'Verify email' },
        component: () => import('modules/storefront/pages/account/verify-email.vue')
      },
      {
        path: 'forgot-password',
        name: 'shop-forgot-password',
        meta: { title: 'Reset password' },
        component: () => import('modules/storefront/pages/account/forgot-password.vue')
      },
      {
        path: 'reset-password',
        name: 'shop-reset-password',
        meta: { title: 'Choose a new password' },
        component: () => import('modules/storefront/pages/account/reset-password.vue')
      },

      // ---- Customer account (authenticated) — WO-21 ----------------------
      {
        path: 'account',
        component: () => import('modules/storefront/pages/account/account.vue'),
        beforeEnter: requireCustomer,
        children: [
          { path: '', redirect: { name: 'shop-account-profile' } },
          {
            path: 'profile',
            name: 'shop-account-profile',
            meta: { title: 'My profile' },
            component: () => import('modules/storefront/pages/account/profile.vue')
          },
          {
            path: 'addresses',
            name: 'shop-account-addresses',
            meta: { title: 'My addresses' },
            component: () => import('modules/storefront/pages/account/addresses.vue')
          },
          {
            path: 'orders',
            name: 'shop-account-orders',
            meta: { title: 'My orders' },
            component: () => import('modules/storefront/pages/account/orders.vue')
          }
        ]
      }
    ]
  }
]
