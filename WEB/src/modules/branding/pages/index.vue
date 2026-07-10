<template>
  <q-page class="app-page">
    <AppDetailHeader
      title="Branding"
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Branding' }]"
      :show-back="false"
    >
      <template #actions>
        <q-btn flat color="primary" icon="o_refresh" label="Reload" no-caps :loading="loading" @click="load" />
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" color="primary" />

    <q-card flat bordered class="app-section">
      <q-tabs v-model="tab" align="left" active-color="primary" indicator-color="primary" class="text-grey-7 app-detail-tabs" no-caps inline-label>
        <q-tab name="identity" icon="o_badge" label="Identity & colours" />
        <q-tab name="media" icon="o_image" label="Logo & favicon" />
        <q-tab name="contact" icon="o_contact_mail" label="Contact & social" />
      </q-tabs>
      <q-separator />

      <q-tab-panels v-model="tab" animated keep-alive>
        <q-tab-panel name="identity" class="q-gutter-y-sm">
          <AppTextField v-model="form.brandName" label="Brand name" required :v="v$.brandName" maxlength="200" :disable="!canWrite" />
          <AppTextField v-model="form.domain" label="Domain" :v="v$.domain" placeholder="store.example.com" maxlength="255" :disable="!canWrite" />
          <AppTextField v-model="form.fontFamily" label="Font family" placeholder="Poppins, Roboto, sans-serif" :disable="!canWrite" />
          <AppSelect v-model="form.defaultLanguage" label="Default language" :options="languageOptions" clearable :disable="!canWrite" />
          <AppSelect
            v-model="form.displayTimeZone"
            label="Display timezone"
            :options="tzOptions"
            use-input
            hide-selected
            fill-input
            input-debounce="0"
            :disable="!canWrite"
            hint="Timezone used to display all UTC dates & times across the admin app and storefront."
            @filter="filterTz"
          />

          <q-separator class="q-my-sm" />
          <div class="text-subtitle2 text-grey-8 q-mb-sm">Colours</div>
          <ColorField v-model="form.primaryColor" label="Primary colour" :v="v$.primaryColor" :disable="!canWrite" />
          <ColorField v-model="form.secondaryColor" label="Secondary colour" :v="v$.secondaryColor" :disable="!canWrite" />
          <ColorField v-model="form.accentColor" label="Accent colour" :v="v$.accentColor" :disable="!canWrite" />
        </q-tab-panel>

        <q-tab-panel name="media" class="q-gutter-y-sm">
          <AppFileUpload media v-model="form.logoMediaId" v-model:preview-url="form.logoUrl" label="Logo" accept="image/*" extensions-label="PNG, JPG, SVG" :disable="!canWrite" />
          <AppFileUpload media v-model="form.faviconMediaId" v-model:preview-url="form.faviconUrl" label="Favicon" accept="image/*,.ico" extensions-label="PNG, ICO, SVG" :disable="!canWrite" />
        </q-tab-panel>

        <q-tab-panel name="contact" class="q-gutter-y-sm">
          <div class="text-subtitle2 text-grey-8 q-mb-sm">Contact</div>
          <AppTextField v-model="form.supportEmail" label="Support email" :v="v$.supportEmail" type="email" :disable="!canWrite" />
          <AppTextField v-model="form.supportPhone" label="Support phone" :v="v$.supportPhone" :disable="!canWrite" />

          <q-separator class="q-my-sm" />
          <div class="text-subtitle2 text-grey-8 q-mb-sm">Social links</div>
          <AppTextField v-model="form.facebook" label="Facebook" :v="v$.facebook" placeholder="https://…" :disable="!canWrite" />
          <AppTextField v-model="form.instagram" label="Instagram" :v="v$.instagram" placeholder="https://…" :disable="!canWrite" />
          <AppTextField v-model="form.twitter" label="X (Twitter)" :v="v$.twitter" placeholder="https://…" :disable="!canWrite" />
          <AppTextField v-model="form.linkedin" label="LinkedIn" :v="v$.linkedin" placeholder="https://…" :disable="!canWrite" />
          <AppTextField v-model="form.youtube" label="YouTube" :v="v$.youtube" placeholder="https://…" :disable="!canWrite" />
        </q-tab-panel>
      </q-tab-panels>

      <template v-if="canWrite">
        <q-separator />
        <q-card-actions class="q-pa-md">
          <div class="text-caption text-grey-7">Save your branding changes — applied across the storefront and admin shell.</div>
          <q-space />
          <q-btn unelevated color="primary" no-caps icon="o_save" label="Save branding" :loading="saving" @click="onSave" />
        </q-card-actions>
      </template>
    </q-card>

    <q-banner v-if="!canWrite" class="bg-grey-2 rounded-borders q-mt-md text-grey-8">
      You have read-only access to branding.
    </q-banner>
  </q-page>
</template>

<script setup>
/*
 * Branding page (WO-9 REQ-TEN-001): the deployment's singleton branding as a full-page editor with an
 * explicit Save (branding is a single upserted record — no list/create). Persists via PUT
 * /api/tenant/branding, then refreshes the global tenant branding (CSS vars + shell brand name/logo).
 */
import { reactive, ref, computed, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength, url, email, hexColor } from 'validators'
import { brandingApi } from 'modules/branding/api'
import { timeZoneOptions } from 'src/utils/datetime'
import { getApiErrorMessage } from 'services/api'
import { useTenantStore } from 'stores/tenant'
import { useNotify } from 'composables/useNotify'
import { usePermissions, Permissions } from 'composables/usePermissions'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFileUpload from 'components/common/AppFileUpload.vue'
import ColorField from 'modules/branding/components/ColorField.vue'

const tenant = useTenantStore()
const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has(Permissions.BrandingWrite))

const tab = ref('identity')
const loading = ref(false)
const saving = ref(false)

const SOCIAL_KEYS = ['facebook', 'instagram', 'twitter', 'linkedin', 'youtube']
const EMPTY = {
  brandName: '', domain: '', fontFamily: '', defaultLanguage: null, displayTimeZone: 'UTC',
  primaryColor: '', secondaryColor: '', accentColor: '',
  logoMediaId: null, logoUrl: '', faviconMediaId: null, faviconUrl: '',
  supportEmail: '', supportPhone: '',
  facebook: '', instagram: '', twitter: '', linkedin: '', youtube: ''
}
const form = reactive({ ...EMPTY })
const extraSocial = ref({})
const layoutOptionsJson = ref(null)

const rules = {
  brandName: { required, maxLength: maxLength(200) },
  domain: { maxLength: maxLength(255) },
  primaryColor: { hexColor, maxLength: maxLength(32) },
  secondaryColor: { hexColor, maxLength: maxLength(32) },
  accentColor: { hexColor, maxLength: maxLength(32) },
  supportEmail: { email },
  supportPhone: { maxLength: maxLength(50) },
  facebook: { url }, instagram: { url }, twitter: { url }, linkedin: { url }, youtube: { url }
}
const v$ = useVuelidate(rules, form)

const TZ_BASE = timeZoneOptions()
const tzOptions = ref(TZ_BASE)
function filterTz (needle, update) {
  const q = (needle || '').toLowerCase().trim()
  update(() => { tzOptions.value = !q ? TZ_BASE : TZ_BASE.filter((o) => o.label.toLowerCase().includes(q)) })
}

const languageOptions = [
  { label: 'English', value: 'en' }, { label: 'Spanish', value: 'es' }, { label: 'French', value: 'fr' },
  { label: 'German', value: 'de' }, { label: 'Portuguese', value: 'pt' }, { label: 'Italian', value: 'it' },
  { label: 'Dutch', value: 'nl' }, { label: 'Arabic', value: 'ar' }, { label: 'Hindi', value: 'hi' },
  { label: 'Chinese (Simplified)', value: 'zh' }, { label: 'Japanese', value: 'ja' }
]

function parseSocial (json) {
  const known = {}; const extra = {}
  if (!json) return { known, extra }
  let obj = null
  try { obj = JSON.parse(json) } catch (e) { return { known, extra } }
  if (!obj || typeof obj !== 'object') return { known, extra }
  for (const [key, value] of Object.entries(obj)) {
    if (SOCIAL_KEYS.includes(key)) known[key] = value
    else extra[key] = value
  }
  return { known, extra }
}

function hydrate (b) {
  Object.assign(form, EMPTY, {
    brandName: b.brandName || '', domain: b.domain || '', fontFamily: b.fontFamily || '',
    defaultLanguage: b.defaultLanguage || null,
    displayTimeZone: b.displayTimeZone || 'UTC',
    primaryColor: b.primaryColor || '', secondaryColor: b.secondaryColor || '', accentColor: b.accentColor || '',
    logoMediaId: b.logoMediaId || null, logoUrl: b.logoUrl || '',
    faviconMediaId: b.faviconMediaId || null, faviconUrl: b.faviconUrl || '',
    supportEmail: b.supportEmail || '', supportPhone: b.supportPhone || ''
  })
  const { known, extra } = parseSocial(b.socialLinksJson)
  for (const key of SOCIAL_KEYS) form[key] = known[key] || ''
  extraSocial.value = extra
  layoutOptionsJson.value = b.layoutOptionsJson || null
  v$.value.$reset()
}

function toNull (value) { const s = (value ?? '').toString().trim(); return s || null }
function buildSocialJson () {
  const out = { ...extraSocial.value }
  for (const key of SOCIAL_KEYS) { const val = (form[key] || '').trim(); if (val) out[key] = val; else delete out[key] }
  return Object.keys(out).length ? JSON.stringify(out) : null
}

async function load () {
  loading.value = true
  try {
    hydrate((await brandingApi.get()) || {})
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function onSave () {
  const ok = await v$.value.$validate()
  if (!ok) { notify.warning('Fix the errors first'); return }
  saving.value = true
  try {
    const updated = await brandingApi.update({
      brandName: form.brandName.trim(),
      domain: toNull(form.domain),
      logoMediaId: form.logoMediaId || null,
      faviconMediaId: form.faviconMediaId || null,
      logoUrl: form.logoMediaId ? null : toNull(form.logoUrl),
      faviconUrl: form.faviconMediaId ? null : toNull(form.faviconUrl),
      primaryColor: toNull(form.primaryColor),
      secondaryColor: toNull(form.secondaryColor),
      accentColor: toNull(form.accentColor),
      fontFamily: toNull(form.fontFamily),
      supportEmail: toNull(form.supportEmail),
      supportPhone: toNull(form.supportPhone),
      socialLinksJson: buildSocialJson(),
      layoutOptionsJson: layoutOptionsJson.value || null,
      defaultLanguage: form.defaultLanguage || null,
      displayTimeZone: form.displayTimeZone || 'UTC'
    })
    hydrate(updated || {})
    notify.success('Branding saved')
    await tenant.loadBranding().catch(() => {})
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
.app-detail-tabs {
  :deep(.q-tab) { min-height: 44px; }
}
</style>
