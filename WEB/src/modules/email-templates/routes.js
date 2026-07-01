/* Email Templates module routes (WO-94 Step 12). */
export default [
  {
    path: 'email-templates',
    name: 'email-templates',
    meta: { title: 'Email Templates', permissions: ['EmailTemplates.Read'] },
    component: () => import('modules/email-templates/pages/index.vue')
  }
]
