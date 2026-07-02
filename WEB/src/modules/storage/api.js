/*
 * File Storage resource (WO-89 REQ-TEN-005).
 *
 * The active file-storage provider and its non-secret options are DB-backed
 * platform settings (category "Storage") served by AdminSettingsController. The
 * Azure connection string is a secret and is stored/masked through the Credential
 * Vault under service type "azure-blob" (same handling as the credentials
 * module, AC-TEN-005.3). Connectivity is probed by AdminStorageController.
 *
 *   GET  /api/admin/settings?category=Storage        -> SettingDto[]
 *   PUT  /api/admin/settings                         -> SettingDto[] ({ settings: { key: value } })
 *   GET  /api/tenant/credentials/azure-blob          -> CredentialSummaryDto (404 when unset)
 *   PUT  /api/tenant/credentials/azure-blob          -> CredentialSummaryDto ({ value, description })
 *   POST /api/admin/storage/test-connection          -> ConnectivityTestResult
 */
import { api, unwrap } from 'services/api'

// Provider identifiers must match the storage adapter `Name` values on the API.
export const PROVIDERS = Object.freeze({
  local: 'LocalFilesystem',
  azure: 'AzureBlobStorage'
})

// Platform-setting keys (seeded under category "Storage").
const KEYS = Object.freeze({
  provider: 'storage.provider',
  azureContainer: 'storage.azure.container',
  cdnBaseUrl: 'storage.cdn.base-url'
})

const AZURE_CREDENTIAL = 'azure-blob'

export const storageApi = {
  // Reads the Storage-category settings and the masked Azure connection-string credential.
  async getConfig () {
    const settings = await api.get('/api/admin/settings', { params: { category: 'Storage' } }).then(unwrap)
    const map = {}
    for (const s of Array.isArray(settings) ? settings : []) map[s.key] = s.value

    let connection = { isConfigured: false, maskedValue: null }
    try {
      const cred = await api.get(`/api/tenant/credentials/${AZURE_CREDENTIAL}`).then(unwrap)
      if (cred) connection = { isConfigured: !!cred.isConfigured, maskedValue: cred.maskedValue || null }
    } catch (err) {
      // A missing credential (404) simply means Azure is not configured yet.
      if (!err || !err.response || err.response.status !== 404) throw err
    }

    return {
      provider: map[KEYS.provider] || PROVIDERS.local,
      azureContainer: map[KEYS.azureContainer] || '',
      cdnBaseUrl: map[KEYS.cdnBaseUrl] || '',
      connection
    }
  },

  // Persists the storage settings; when a new connection string is supplied, (re)stores the secret.
  async updateConfig ({ provider, azureContainer, cdnBaseUrl, connectionString }) {
    const settings = { [KEYS.provider]: provider }
    // Only touch the Azure options when Azure is selected so switching to Local
    // leaves the previous Azure configuration intact for a later switch back.
    if (provider === PROVIDERS.azure) {
      settings[KEYS.azureContainer] = azureContainer || ''
      settings[KEYS.cdnBaseUrl] = cdnBaseUrl || ''
    }
    await api.put('/api/admin/settings', { settings }).then(unwrap)

    if (provider === PROVIDERS.azure && connectionString) {
      await api.put(`/api/tenant/credentials/${AZURE_CREDENTIAL}`, {
        value: connectionString,
        description: 'Azure Blob Storage connection string'
      }).then(unwrap)
    }

    return storageApi.getConfig()
  },

  // Write-read-delete probe against the active provider (AC-TEN-005.4/5).
  testConnection () {
    return api.post('/api/admin/storage/test-connection').then(unwrap)
  }
}

export default storageApi
