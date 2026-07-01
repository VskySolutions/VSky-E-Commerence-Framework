/*
 * Widget module routes (WO-94 Step 12) — the template other feature modules
 * copy. Children of the authenticated shell.
 */
export default [
  {
    path: 'widgets',
    name: 'widgets',
    meta: { title: 'Widgets' },
    component: () => import('modules/widget/pages/index.vue')
  },
  {
    path: 'widgets/:id',
    name: 'widget-detail',
    meta: { title: 'Widget' },
    component: () => import('modules/widget/pages/detail.vue')
  }
]
