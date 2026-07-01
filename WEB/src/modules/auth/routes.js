/*
 * Auth module routes (WO-94 Step 12). Own (chrome-less) auth_layout.
 *   /auth/login, /setup, /change-password
 */
export default [
  {
    path: '/auth',
    component: () => import('layouts/auth_layout.vue'),
    children: [
      { path: '', redirect: '/auth/login' },
      {
        path: 'login',
        name: 'login',
        meta: { title: 'Sign in' },
        component: () => import('modules/auth/pages/login.vue')
      }
    ]
  },
  {
    path: '/setup',
    component: () => import('layouts/auth_layout.vue'),
    children: [
      {
        path: '',
        name: 'setup',
        meta: { title: 'Setup' },
        component: () => import('modules/auth/pages/setup.vue')
      }
    ]
  },
  {
    path: '/change-password',
    component: () => import('layouts/auth_layout.vue'),
    children: [
      {
        path: '',
        name: 'change-password',
        meta: { requiresAuth: true, title: 'Change Password' },
        component: () => import('modules/auth/pages/change_password.vue')
      }
    ]
  }
]
