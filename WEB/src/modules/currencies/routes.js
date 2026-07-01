/* Currencies module routes (WO-94 Step 12). */
export default [
  {
    path: 'currencies',
    name: 'currencies',
    meta: { title: 'Currencies', permissions: ['Currencies.Read'] },
    component: () => import('modules/currencies/pages/index.vue')
  }
]
