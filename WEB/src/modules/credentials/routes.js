/* Credentials module routes (WO-94 Step 12). */
export default [
  {
    path: 'credentials',
    name: 'credentials',
    meta: { title: 'Credentials', permissions: ['Credentials.Read'] },
    component: () => import('modules/credentials/pages/index.vue')
  },
  {
    path: 'credentials/new',
    name: 'credential-new',
    meta: { title: 'New credential', permissions: ['Credentials.Write'] },
    component: () => import('modules/credentials/pages/credential-detail.vue')
  },
  {
    path: 'credentials/:id',
    name: 'credential-detail',
    meta: { title: 'Credential', permissions: ['Credentials.Read'] },
    component: () => import('modules/credentials/pages/credential-detail.vue')
  }
]
