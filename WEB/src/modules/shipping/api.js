/*
 * Shipping admin module API layer (WO-116): custom methods + zones.
 * Wraps AdminShippingMethodsController + AdminShippingZonesController.
 */
import { api, unwrap, qsSerializer } from 'services/api'

const METHODS = '/api/admin/shipping-methods'
const ZONES = '/api/admin/shipping-zones'

export const shippingMethodApi = {
  list (params = {}) { return api.get(METHODS, { params, paramsSerializer: qsSerializer }).then(unwrap) },
  get (id) { return api.get(`${METHODS}/${id}`).then(unwrap) },
  create (payload) { return api.post(METHODS, payload).then(unwrap) },
  update (id, payload) { return api.put(`${METHODS}/${id}`, payload).then(unwrap) },
  remove (id) { return api.delete(`${METHODS}/${id}`).then(unwrap) }
}

export const shippingZoneApi = {
  list (params = {}) { return api.get(ZONES, { params, paramsSerializer: qsSerializer }).then(unwrap) },
  get (id) { return api.get(`${ZONES}/${id}`).then(unwrap) },
  create (payload) { return api.post(ZONES, payload).then(unwrap) },
  update (id, payload) { return api.put(`${ZONES}/${id}`, payload).then(unwrap) },
  remove (id) { return api.delete(`${ZONES}/${id}`).then(unwrap) }
}

export const shippingMethodTypeOptions = [
  { label: 'Flat rate', value: 'FlatRate' },
  { label: 'Weight-based', value: 'WeightBased' },
  { label: 'Price-based', value: 'PriceBased' },
  { label: 'Free shipping', value: 'FreeShipping' }
]

export default { shippingMethodApi, shippingZoneApi, shippingMethodTypeOptions }
