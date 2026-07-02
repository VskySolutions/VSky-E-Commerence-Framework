/*
 * Users module routes (WO-62 / REQ-ADM-004). Child of the authenticated shell.
 * The parent registers this array in router/index.js.
 */
export default [
  {
    path: 'users',
    name: 'users',
    meta: { title: 'Users', permissions: ['Users.Read'] },
    component: () => import('modules/users/pages/index.vue')
  }
]
