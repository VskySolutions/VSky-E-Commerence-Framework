/* Customers admin module routes (WO-117). */
export default [
  {
    path: 'customers',
    name: 'admin-customers',
    meta: { title: 'Customers', permissions: ['Users.Read'] },
    component: () => import('modules/customers/pages/index.vue')
  }
]
