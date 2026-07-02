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
      }
    ]
  }
]
