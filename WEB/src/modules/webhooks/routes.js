/* Webhooks admin module routes (WO-118). */
export default [
  {
    path: 'webhooks',
    name: 'admin-webhooks',
    meta: { title: 'Webhooks', permissions: ['Webhooks.Read'] },
    component: () => import('modules/webhooks/pages/index.vue')
  }
]
