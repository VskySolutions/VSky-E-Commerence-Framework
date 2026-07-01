/* Credentials module routes (WO-94 Step 12). */
export default [
  {
    path: 'credentials',
    name: 'credentials',
    meta: { title: 'Credentials', permissions: ['Credentials.Read'] },
    component: () => import('modules/credentials/pages/index.vue')
  }
]
