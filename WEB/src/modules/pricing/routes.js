/* Pricing/promotions admin module routes (WO-115). */
export default [
  {
    path: 'promotions',
    name: 'admin-promotions',
    meta: { title: 'Promotions', permissions: ['Catalog.Read'] },
    component: () => import('modules/pricing/pages/index.vue')
  }
]
