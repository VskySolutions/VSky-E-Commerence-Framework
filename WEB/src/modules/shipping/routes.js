/* Shipping admin module routes (WO-116). */
export default [
  {
    path: 'shipping',
    name: 'admin-shipping',
    meta: { title: 'Shipping', permissions: ['Stores.Read'] },
    component: () => import('modules/shipping/pages/index.vue')
  }
]
