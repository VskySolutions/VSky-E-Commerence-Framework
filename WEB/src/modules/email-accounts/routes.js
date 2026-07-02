/*
 * Email + SMS accounts module routes (WO-77). The parent shell imports this
 * default-exported array and appends it under the authenticated layout.
 */
export default [
  {
    path: 'email-accounts',
    name: 'email-accounts',
    meta: { title: 'Email & SMS Accounts', permissions: ['SmtpAccounts.Read'] },
    component: () => import('modules/email-accounts/pages/index.vue')
  }
]
