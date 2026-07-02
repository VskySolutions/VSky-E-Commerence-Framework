/* File Storage module routes (WO-89 REQ-TEN-005). Child of the authenticated shell. */
export default [
  {
    path: 'storage',
    name: 'storage',
    meta: { title: 'File Storage', permissions: ['Storage.Read'] },
    component: () => import('modules/storage/pages/index.vue')
  }
]
