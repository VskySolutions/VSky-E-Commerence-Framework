/*
 * Storefront customer account API layer (WO-21).
 *
 * - customerAuthApi: PUBLIC auth endpoints (register/verify/login/reset) on the
 *   ANONYMOUS instance, plus the shared refresh endpoint.
 * - accountApi: the AUTHENTICATED customer endpoints (profile, addresses, orders)
 *   on the isolated storefront customer instance (`customerApi`), which carries the
 *   customer bearer token from a LocalStorage slot separate from the admin session.
 *
 * JSON is camelCase; enums serialize as string names (AddressType = Shipping|Billing).
 */
import { customerApi, anonApi, unwrap, qsSerializer } from 'services/api'

const AUTH = '/api/customer/auth'
const PROFILE = '/api/customer/profile'
const ADDRESSES = '/api/customer/addresses'
const ORDERS = '/api/customer/orders'

export const customerAuthApi = {
  // Create an account; a verification email is sent. Returns the new customer id.
  register (payload) {
    return anonApi.post(AUTH + '/register', payload).then(unwrap)
  },
  // Confirm an email address with the token from the verification link.
  verifyEmail (token) {
    return anonApi.post(AUTH + '/verify-email', { token }).then(unwrap)
  },
  // Authenticate; returns { accessToken, expiresAtUtc, refreshToken, user }.
  login (payload) {
    return anonApi.post(AUTH + '/login', payload).then(unwrap)
  },
  // Start the reset flow (always succeeds, even for unknown emails).
  requestPasswordReset (payload) {
    return anonApi.post(AUTH + '/request-password-reset', payload).then(unwrap)
  },
  // Set a new password using a valid reset token.
  resetPassword (token, newPassword) {
    return anonApi.post(AUTH + '/reset-password', { token, newPassword }).then(unwrap)
  },
  // Silent token refresh (the refresh endpoint is shared with the admin app).
  refresh (refreshToken) {
    return anonApi.post('/api/auth/refresh', { refreshToken }).then(unwrap)
  }
}

export const accountApi = {
  // ---- Profile ----
  getProfile () {
    return customerApi.get(PROFILE).then(unwrap)
  },
  updateProfile ({ firstName, lastName, phoneNumber }) {
    return customerApi.put(PROFILE, { firstName, lastName, phoneNumber }).then(unwrap)
  },
  changeEmail (newEmail) {
    return customerApi.put(PROFILE + '/email', { newEmail }).then(unwrap)
  },
  changePassword ({ currentPassword, newPassword }) {
    return customerApi.put(PROFILE + '/password', { currentPassword, newPassword }).then(unwrap)
  },

  // ---- Address book ----
  // Grouped by type ({ shipping:[], billing:[] }) for the address-book UI.
  addressBook () {
    return customerApi.get(ADDRESSES, { params: { grouped: true }, paramsSerializer: qsSerializer }).then(unwrap)
  },
  createAddress (payload) {
    return customerApi.post(ADDRESSES, payload).then(unwrap)
  },
  updateAddress (id, payload) {
    return customerApi.put(ADDRESSES + '/' + encodeURIComponent(id), payload).then(unwrap)
  },
  removeAddress (id) {
    return customerApi.delete(ADDRESSES + '/' + encodeURIComponent(id)).then(unwrap)
  },

  // ---- Order history ----
  orders (params = {}) {
    return customerApi.get(ORDERS, { params, paramsSerializer: qsSerializer }).then(unwrap)
  }
}

export default { customerAuthApi, accountApi }
