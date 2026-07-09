<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="isCreate ? 'New credential' : (entity?.serviceType || 'Credential')"
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Credentials', to: { name: 'credentials' } },
        { label: isCreate ? 'New credential' : (entity?.serviceType || 'Credential') }
      ]"
      :status="!isCreate && entity ? (entity.isConfigured ? 'Configured' : 'Empty') : ''"
      :status-color="entity?.isConfigured ? 'positive' : 'grey'"
      show-back
      @back="router.push({ name: 'credentials' })"
    />

    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !isCreate && !entity" class="bg-grey-2 rounded-borders">
      Credential not found.
    </q-banner>

    <template v-if="isCreate || entity">
      <q-card flat bordered class="app-section">
        <q-tabs v-model="tab" align="left" active-color="primary" indicator-color="primary" class="text-grey-7 app-detail-tabs" no-caps inline-label>
          <q-tab name="general" icon="o_key" label="General" />
        </q-tabs>
        <q-separator />

        <q-tab-panels v-model="tab" animated keep-alive>
          <q-tab-panel name="general" class="q-gutter-y-sm">
            <template v-if="isCreate">
              <AppSelect v-model="form.serviceChoice" label="Service type" required :options="serviceOptions" :v="v$.serviceChoice" />
              <AppTextField v-if="form.serviceChoice === CUSTOM" v-model="form.customServiceType" label="Custom service type" required :v="v$.customServiceType" placeholder="e.g. mailchimp" maxlength="100" />
            </template>
            <template v-else>
              <AppTextField :model-value="form.serviceType" label="Service type" disable />
              <div v-if="entity?.maskedValue" class="text-caption text-grey-7 q-mb-xs">Current value: <span class="cred-value">{{ entity.maskedValue }}</span></div>
            </template>

            <AppTextField v-model="form.secret" label="Secret value" required :v="v$.secret" :type="showValue ? 'text' : 'password'" autocomplete="new-password">
              <template #hint><span v-if="!isCreate" class="text-caption text-grey-7">Replaces the stored secret (secrets are never shown)</span></template>
              <template #append><q-icon :name="showValue ? 'o_visibility_off' : 'o_visibility'" class="cursor-pointer" @click="showValue = !showValue" /></template>
            </AppTextField>

            <AppTextField v-model="form.description" label="Description" type="textarea" autogrow maxlength="500" :v="v$.description" />
          </q-tab-panel>
        </q-tab-panels>

        <q-separator />
        <q-card-actions class="q-pa-md">
          <div class="text-caption text-grey-7">{{ isCreate ? 'Create this credential.' : 'Re-enter the secret to save changes.' }}</div>
          <q-space />
          <q-btn v-if="canWrite" unelevated color="primary" no-caps :icon="isCreate ? 'o_check' : 'o_save'" :label="isCreate ? 'Create credential' : 'Save'" :loading="saving > 0 || creating" @click="save" />
        </q-card-actions>
      </q-card>
    </template>
  </q-page>
</template>

<script setup>
/* Credential create + edit page (full-page, explicit Save). Keyed by serviceType (upsert); the secret
 * is write-only, so it must be (re-)entered to save. */
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useDetailForm } from 'composables/useDetailForm'
import { required, requiredIf, maxLength } from 'validators'
import { credentialApi } from 'modules/credentials/api'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'

const route = useRoute()
const router = useRouter()
const { has } = usePermissions()
const canWrite = computed(() => has(Permissions.CredentialsWrite))

const CUSTOM = '__custom__'
const tab = ref('general')
const showValue = ref(false)
const isNew = computed(() => route.name === 'credential-new')

const serviceOptions = [
  { label: 'Stripe', value: 'stripe' }, { label: 'Stripe Tax', value: 'stripe-tax' }, { label: 'PayPal', value: 'paypal' },
  { label: 'Razorpay', value: 'razorpay' }, { label: 'Square', value: 'square' }, { label: 'Authorize.Net', value: 'authorizenet' },
  { label: 'TaxJar', value: 'taxjar' }, { label: 'DHL', value: 'dhl' }, { label: 'UPS', value: 'ups' }, { label: 'FedEx', value: 'fedex' },
  { label: 'USPS', value: 'usps' }, { label: 'SMTP', value: 'smtp' }, { label: 'Azure Blob Storage', value: 'azure-blob' },
  { label: 'Other…', value: CUSTOM }
]

function resolvedType (f) {
  return f.serviceType || (f.serviceChoice === CUSTOM ? (f.customServiceType || '').trim() : f.serviceChoice)
}

function buildPayload (f) {
  return { serviceType: resolvedType(f), secret: f.secret, description: (f.description || '').trim() || null }
}

// Both create and edit are PUT (upsert) by serviceType; the secret maps to `value`.
const detailApi = {
  get: (st) => credentialApi.get(st),
  create: (p) => credentialApi.upsert(p.serviceType, { value: p.secret, description: p.description }).then(() => ({ serviceType: p.serviceType })),
  update: (st, p) => credentialApi.upsert(st, { value: p.secret, description: p.description }).then(() => credentialApi.get(st))
}

const rules = computed(() => ({
  serviceChoice: { requiredIf: requiredIf(() => isNew.value) },
  customServiceType: { requiredIf: requiredIf(() => isNew.value && form.serviceChoice === CUSTOM), maxLength: maxLength(100) },
  secret: { required },
  description: { maxLength: maxLength(500) }
}))

const {
  form, v$, entity, loading, creating, saving, isCreate, save
} = useDetailForm({
  createRouteName: 'credential-new',
  detailRouteName: 'credential-detail',
  entityLabel: 'credential',
  autoSave: false,
  idField: 'serviceType',
  api: detailApi,
  buildPayload,
  empty: { serviceType: '', serviceChoice: '', customServiceType: '', secret: '', description: '' },
  rules,
  hydrateForm: (f, e) => { f.serviceType = e.serviceType || ''; f.secret = ''; f.description = e.description || '' }
})
</script>

<style scoped lang="scss">
.app-detail-tabs {
  :deep(.q-tab) { min-height: 44px; }
}
.cred-value { font-family: 'Roboto Mono', monospace; letter-spacing: 1px; }
</style>
