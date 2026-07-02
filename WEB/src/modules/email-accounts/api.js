/*
 * Email + SMS account APIs (WO-77).
 *
 * smtpAccountApi -> TenantSmtpAccountsController (/api/tenant/smtp-accounts).
 *   Passwords are write-only: never returned; a blank password on update leaves
 *   the stored one unchanged. Enums travel as their exact member-name strings
 *   (the API registers JsonStringEnumConverter), e.g. encryptionMode: 'StartTls',
 *   authMethod: 'Auto', category: 'Transactional' | 'Marketing' | null.
 *
 *     GET    /api/tenant/smtp-accounts                 -> SmtpAccountDto[]
 *     GET    /api/tenant/smtp-accounts/{id}            -> SmtpAccountDto
 *     POST   /api/tenant/smtp-accounts                 -> SmtpAccountDto
 *     PUT    /api/tenant/smtp-accounts/{id}            -> SmtpAccountDto
 *     DELETE /api/tenant/smtp-accounts/{id}            -> 204
 *     POST   /api/tenant/smtp-accounts/{id}/test-send  ({ toEmail }) -> ConnectivityTestResult
 *
 * twilioApi -> the shared credential vault (TenantCredentialsController), service
 *   type "twilio". No dedicated Twilio controller exists, so the auth token is the
 *   encrypted secret (`value`, masked on read) and the non-secret Account SID /
 *   From Number / enabled flag round-trip through the credential `description`
 *   field as JSON (encoded/decoded in the page). GET 404s until first configured.
 *
 *     GET    /api/tenant/credentials/twilio        -> CredentialSummaryDto
 *     PUT    /api/tenant/credentials/twilio        ({ value, description }) -> CredentialSummaryDto
 *     POST   /api/tenant/credentials/twilio/test   -> ConnectivityTestResult
 *     DELETE /api/tenant/credentials/twilio        -> 204
 */
import { api, unwrap, qsSerializer } from 'services/api'

const SMTP_BASE = '/api/tenant/smtp-accounts'
export const TWILIO_SERVICE_TYPE = 'twilio'
const TWILIO_BASE = `/api/tenant/credentials/${TWILIO_SERVICE_TYPE}`

export const smtpAccountApi = {
  list (params = {}) {
    return api.get(SMTP_BASE, { params, paramsSerializer: qsSerializer }).then(unwrap)
  },
  get (id) {
    return api.get(`${SMTP_BASE}/${id}`).then(unwrap)
  },
  create (payload) {
    return api.post(SMTP_BASE, payload).then(unwrap)
  },
  update (id, payload) {
    return api.put(`${SMTP_BASE}/${id}`, payload).then(unwrap)
  },
  remove (id) {
    return api.delete(`${SMTP_BASE}/${id}`).then(unwrap)
  },
  testSend (id, toEmail) {
    return api.post(`${SMTP_BASE}/${id}/test-send`, { toEmail }).then(unwrap)
  }
}

export const twilioApi = {
  get () {
    return api.get(TWILIO_BASE).then(unwrap)
  },
  upsert (payload) {
    return api.put(TWILIO_BASE, payload).then(unwrap)
  },
  test () {
    return api.post(`${TWILIO_BASE}/test`).then(unwrap)
  },
  remove () {
    return api.delete(TWILIO_BASE).then(unwrap)
  }
}

export default { smtpAccountApi, twilioApi }
