<template>
  <q-page class="app-page">
    <AppListHeader
      title="File Storage"
      subtitle="Choose where uploaded files are stored and verify connectivity."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'File Storage' }]"
    >
      <template #actions>
        <q-btn flat color="primary" icon="o_refresh" label="Reload" no-caps :loading="loading" @click="load" />
      </template>
    </AppListHeader>

    <!-- AC-TEN-005.5: the active provider is unreachable -> new uploads will fail. -->
    <q-banner v-if="unreachable" rounded class="bg-negative text-white q-mb-md">
      <template #avatar><q-icon name="o_error_outline" /></template>
      The active storage provider ({{ providerLabel(savedProvider) }}) is unreachable. New file uploads
      will be rejected until connectivity is restored.
      <div v-if="testResult && testResult.message" class="text-caption q-mt-xs">{{ testResult.message }}</div>
    </q-banner>

    <div class="row q-col-gutter-md">
      <!-- Configuration -->
      <div class="col-12 col-md-7">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Storage provider</div>

            <!-- AC-TEN-005.1: provider selection. -->
            <q-option-group
              v-model="form.provider"
              type="radio"
              color="primary"
              :options="providerOptions"
            />

            <p v-if="!isAzure" class="text-body2 text-grey-7 q-mt-sm q-mb-none">
              Files are stored on the API host's local filesystem and served via the static-file path.
            </p>

            <!-- AC-TEN-005.6: switching providers does not migrate existing files. -->
            <q-banner v-if="providerChanged" dense rounded class="bg-blue-1 text-blue-9 q-mt-md">
              <template #avatar><q-icon name="o_info" color="blue-9" /></template>
              Existing files stored under the previous provider are not migrated automatically.
            </q-banner>
          </q-card-section>

          <!-- AC-TEN-005.2/3: Azure Blob configuration. -->
          <template v-if="isAzure">
            <q-separator />
            <q-card-section>
              <div class="text-subtitle1 q-mb-sm">Azure Blob Storage</div>

              <AppTextField
                v-model="form.connectionString"
                label="Connection string"
                :type="showSecret ? 'text' : 'password'"
                :required="connectionRequired"
                :v="v$.connectionString"
                autocomplete="new-password"
                :placeholder="connection.isConfigured ? 'Leave blank to keep the stored value' : 'DefaultEndpointsProtocol=https;AccountName=…'"
              >
                <template #hint>
                  <span v-if="connection.isConfigured" class="text-caption text-grey-7">
                    Stored: {{ connection.maskedValue || '••••' }} — encrypted at rest. Re-enter to replace it.
                  </span>
                  <span v-else class="text-caption text-grey-7">Encrypted at rest and shown masked after saving.</span>
                </template>
                <template #append>
                  <q-icon
                    :name="showSecret ? 'o_visibility_off' : 'o_visibility'"
                    class="cursor-pointer"
                    @click="showSecret = !showSecret"
                  />
                </template>
              </AppTextField>

              <AppTextField
                v-model="form.azureContainer"
                label="Container name"
                :required="isAzure"
                :v="v$.azureContainer"
                placeholder="uploads"
              />

              <AppTextField
                v-model="form.cdnBaseUrl"
                label="CDN base URL"
                :v="v$.cdnBaseUrl"
                placeholder="https://cdn.example.com"
              >
                <template #hint>
                  <span class="text-caption text-grey-7">Optional. Prefixed to stored asset URLs when serving files.</span>
                </template>
              </AppTextField>
            </q-card-section>
          </template>

          <q-separator />
          <q-card-actions class="q-pa-md">
            <q-btn
              unelevated
              color="primary"
              icon="o_save"
              label="Save changes"
              no-caps
              :loading="saving"
              @click="onSave"
            />
            <q-btn
              flat
              color="primary"
              icon="o_wifi_tethering"
              label="Test connection"
              no-caps
              :loading="testing"
              @click="onTest()"
            />
          </q-card-actions>
        </q-card>
      </div>

      <!-- Connection status (AC-TEN-005.4). -->
      <div class="col-12 col-md-5">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Connection status</div>

            <div class="row items-center q-mb-sm">
              <div class="col text-body2 text-grey-7">Active provider</div>
              <q-badge color="primary" :label="providerLabel(savedProvider)" />
            </div>

            <q-separator class="q-my-sm" />

            <div v-if="testing" class="row items-center text-grey-7">
              <q-spinner size="18px" class="q-mr-sm" /> Checking connectivity…
            </div>
            <template v-else-if="testResult">
              <div class="row items-center no-wrap">
                <q-icon
                  :name="testResult.success ? 'o_check_circle' : 'o_cancel'"
                  :color="testResult.success ? 'positive' : 'negative'"
                  size="20px"
                  class="q-mr-sm"
                />
                <div class="col">
                  <div class="text-body2" :class="testResult.success ? 'text-positive' : 'text-negative'">
                    {{ testResult.success ? 'Reachable' : 'Unreachable' }}
                  </div>
                  <div class="text-caption text-grey-7">{{ testResult.message }}</div>
                  <div v-if="testResult.testedAtUtc" class="text-caption text-grey-6">Checked {{ fmtDate(testResult.testedAtUtc) }}</div>
                </div>
              </div>
            </template>
            <div v-else class="text-body2 text-grey-7">
              Connectivity has not been checked yet. Use “Test connection” to run a write-read-delete probe
              against the active provider.
            </div>

            <div class="text-caption text-grey-6 q-mt-md">
              The test verifies the currently active (saved) provider. Save your changes first, then test to
              validate a newly selected provider.
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup>
/*
 * File Storage settings panel (WO-89 REQ-TEN-005). Selects the active storage
 * provider (Local / Azure Blob), configures Azure connection string (masked,
 * re-entry to change), container and optional CDN URL, and probes connectivity.
 * The connection string is handled as a Credential Vault secret (AC-TEN-005.3).
 */
import { reactive, ref, computed, onMounted } from 'vue'
import { format, parseISO, isValid } from 'date-fns'
import useVuelidate from '@vuelidate/core'
import { requiredIf, maxLength, url } from 'validators'
import { storageApi, PROVIDERS } from 'modules/storage/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'

const notify = useNotify()

const providerOptions = [
  { label: 'Local Filesystem', value: PROVIDERS.local },
  { label: 'Azure Blob Storage', value: PROVIDERS.azure }
]

const EMPTY = { provider: PROVIDERS.local, azureContainer: '', cdnBaseUrl: '', connectionString: '' }
const form = reactive({ ...EMPTY })

const savedProvider = ref(PROVIDERS.local)
const connection = ref({ isConfigured: false, maskedValue: null })
const testResult = ref(null)

const loading = ref(false)
const saving = ref(false)
const testing = ref(false)
const showSecret = ref(false)

const isAzure = computed(() => form.provider === PROVIDERS.azure)
const providerChanged = computed(() => form.provider !== savedProvider.value)
const unreachable = computed(() => !!(testResult.value && testResult.value.success === false))
// A connection string must be entered the first time Azure is configured; once
// stored it may be left blank to keep the existing secret.
const connectionRequired = computed(() => isAzure.value && !connection.value.isConfigured)

const rules = computed(() => ({
  connectionString: { requiredIf: requiredIf(() => connectionRequired.value) },
  azureContainer: { requiredIf: requiredIf(() => isAzure.value), maxLength: maxLength(63) },
  cdnBaseUrl: { url }
}))
const v$ = useVuelidate(rules, form)

function providerLabel (value) {
  return providerOptions.find((o) => o.value === value)?.label || value || '—'
}

function fmtDate (value) {
  if (!value) return '—'
  const d = typeof value === 'string' ? parseISO(value) : new Date(value)
  return isValid(d) ? format(d, 'dd MMM yyyy, HH:mm') : '—'
}

function applyConfig (cfg) {
  savedProvider.value = cfg.provider
  connection.value = cfg.connection || { isConfigured: false, maskedValue: null }
  Object.assign(form, {
    provider: cfg.provider,
    azureContainer: cfg.azureContainer,
    cdnBaseUrl: cfg.cdnBaseUrl,
    connectionString: ''
  })
  v$.value.$reset()
}

async function load () {
  loading.value = true
  try {
    applyConfig(await storageApi.getConfig())
    // Surface an unreachable active provider without waiting for the admin (AC-TEN-005.5).
    onTest(true)
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function onSave () {
  const ok = await v$.value.$validate()
  if (!ok) return
  saving.value = true
  try {
    const cfg = await storageApi.updateConfig({
      provider: form.provider,
      azureContainer: form.azureContainer,
      cdnBaseUrl: form.cdnBaseUrl,
      connectionString: form.connectionString || undefined
    })
    applyConfig(cfg)
    notify.success('Storage settings saved')
    // Validate connectivity of the now-active provider (AC-TEN-005.4).
    onTest(true)
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

async function onTest (silent = false) {
  testing.value = true
  try {
    const result = await storageApi.testConnection()
    testResult.value = result || { success: false, message: 'No result returned.', testedAtUtc: new Date().toISOString() }
    if (!silent) {
      if (testResult.value.success) notify.success(testResult.value.message || 'Connection succeeded')
      else notify.error(testResult.value.message || 'Connection failed')
    }
  } catch (err) {
    const message = getApiErrorMessage(err)
    testResult.value = { success: false, message, testedAtUtc: new Date().toISOString() }
    if (!silent) notify.error(message)
  } finally {
    testing.value = false
  }
}

onMounted(load)
</script>
