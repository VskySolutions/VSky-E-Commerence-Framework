/*
 * Customers admin module API layer (WO-117): customer list + role assignment +
 * tax-exemption, plus the customer-role catalog. Wraps AdminCustomersController +
 * AdminCustomerRolesController.
 */
import { api, unwrap, qsSerializer } from 'services/api'

const CUSTOMERS = '/api/admin/customers'

export const customerAdminApi = {
  list (params = {}) { return api.get(CUSTOMERS, { params, paramsSerializer: qsSerializer }).then(unwrap) },
  getRoles (id) { return api.get(`${CUSTOMERS}/${id}/roles`).then(unwrap) },
  setRoles (id, roleIds) { return api.put(`${CUSTOMERS}/${id}/roles`, { customerId: id, roleIds }).then(unwrap) },
  getTaxExemption (id) { return api.get(`${CUSTOMERS}/${id}/tax-exemption`).then(unwrap) },
  setTaxExemption (id, payload) { return api.put(`${CUSTOMERS}/${id}/tax-exemption`, { customerId: id, ...payload }).then(unwrap) }
}

export const customerRoleApi = {
  list (params = {}) { return api.get('/api/admin/customer-roles', { params, paramsSerializer: qsSerializer }).then(unwrap) }
}

export default { customerAdminApi, customerRoleApi }
