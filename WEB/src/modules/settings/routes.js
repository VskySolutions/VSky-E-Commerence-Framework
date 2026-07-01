/* Settings module routes (WO-94 Step 12). */
export default [
  {
    path: 'settings',
    name: 'settings',
    meta: { title: 'Settings', permissions: ['Settings.Read'] },
    component: () => import('modules/settings/pages/index.vue')
  }
]
