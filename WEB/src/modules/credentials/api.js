/*
 * Credentials resource (WO-9 REQ-TEN-002).
 *
 * Manages the deployment's encrypted third-party credentials, keyed by
 * serviceType. Values are only ever returned MASKED (last four characters).
 * Mirrors the widgetApi template in services/api.js.
 *
 *   GET    /api/tenant/credentials                  -> CredentialSummaryDto[]
 *   GET    /api/tenant/credentials/{serviceType}    -> CredentialSummaryDto
 *   PUT    /api/tenant/credentials/{serviceType}    -> CredentialSummaryDto ({ value, description })
 *   POST   /api/tenant/credentials/{serviceType}/test -> ConnectivityTestResult
 *   DELETE /api/tenant/credentials/{serviceType}    -> 204
 */
import { api, unwrap } from 'services/api'

export const credentialApi = {
  list () {
    return api.get('/api/tenant/credentials').then(unwrap)
  },
  get (serviceType) {
    return api.get(`/api/tenant/credentials/${encodeURIComponent(serviceType)}`).then(unwrap)
  },
  upsert (serviceType, payload) {
    return api.put(`/api/tenant/credentials/${encodeURIComponent(serviceType)}`, payload).then(unwrap)
  },
  test (serviceType) {
    return api.post(`/api/tenant/credentials/${encodeURIComponent(serviceType)}/test`).then(unwrap)
  },
  remove (serviceType) {
    return api.delete(`/api/tenant/credentials/${encodeURIComponent(serviceType)}`).then(unwrap)
  }
}

export default credentialApi
