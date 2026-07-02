/*
 * Roles module routes (WO-62 / REQ-ADM-004). Child of the authenticated shell.
 * The parent registers this array in router/index.js.
 */
export default [
  {
    path: 'roles',
    name: 'roles',
    meta: { title: 'Roles', permissions: ['Roles.Read'] },
    component: () => import('modules/roles/pages/index.vue')
  }
]
