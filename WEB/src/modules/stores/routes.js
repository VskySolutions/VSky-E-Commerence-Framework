/* Stores module routes (WO-94 Step 12). */
export default [
  {
    path: 'stores',
    name: 'stores',
    meta: { title: 'Stores', permissions: ['Stores.Read'] },
    component: () => import('modules/stores/pages/index.vue')
  }
]
