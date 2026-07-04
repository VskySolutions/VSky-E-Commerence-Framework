<template>
  <AppFormDrawer
    :model-value="modelValue"
    title="Edit branding"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <div class="text-subtitle2 text-grey-8 q-mb-sm">Identity</div>
    <AppTextField v-model="form.brandName" label="Brand name" required :v="v$.brandName" maxlength="200" />
    <AppTextField v-model="form.domain" label="Domain" :v="v$.domain" placeholder="store.example.com" maxlength="255" />
    <AppTextField v-model="form.fontFamily" label="Font family" placeholder="Poppins, Roboto, sans-serif" />
    <AppSelect
      v-model="form.defaultLanguage"
      label="Default language"
      :options="languageOptions"
      clearable
    />

    <q-separator class="q-my-md" />
    <div class="text-subtitle2 text-grey-8 q-mb-sm">Colours</div>
    <ColorField v-model="form.primaryColor" label="Primary colour" :v="v$.primaryColor" />
    <ColorField v-model="form.secondaryColor" label="Secondary colour" :v="v$.secondaryColor" />
    <ColorField v-model="form.accentColor" label="Accent colour" :v="v$.accentColor" />

    <q-separator class="q-my-md" />
    <div class="text-subtitle2 text-grey-8 q-mb-sm">Logo &amp; favicon</div>
    <AppFileUpload v-model="form.logoUrl" label="Logo" folder="branding" accept="image/*" extensions-label="PNG, JPG, SVG" />
    <AppFileUpload v-model="form.faviconUrl" label="Favicon" folder="branding" accept="image/*,.ico" extensions-label="PNG, ICO, SVG" />

    <q-separator class="q-my-md" />
    <div class="text-subtitle2 text-grey-8 q-mb-sm">Contact</div>
    <AppTextField v-model="form.supportEmail" label="Support email" :v="v$.supportEmail" type="email" />
    <AppTextField v-model="form.supportPhone" label="Support phone" :v="v$.supportPhone" />

    <q-separator class="q-my-md" />
    <div class="text-subtitle2 text-grey-8 q-mb-sm">Social links</div>
    <AppTextField v-model="form.facebook" label="Facebook" :v="v$.facebook" placeholder="https://…" />
    <AppTextField v-model="form.instagram" label="Instagram" :v="v$.instagram" placeholder="https://…" />
    <AppTextField v-model="form.twitter" label="X (Twitter)" :v="v$.twitter" placeholder="https://…" />
    <AppTextField v-model="form.linkedin" label="LinkedIn" :v="v$.linkedin" placeholder="https://…" />
    <AppTextField v-model="form.youtube" label="YouTube" :v="v$.youtube" placeholder="https://…" />
  </AppFormDrawer>
</template>

<script setup>
/*
 * BrandingFormDrawer (WO-9 REQ-TEN-001): a Vuelidate-validated editor for the
 * deployment's singleton branding, inside AppFormDrawer. Emits `submit` with a
 * payload shaped exactly like the backend UpdateBrandingCommand.
 *
 * - Social links are edited as friendly per-platform URL fields but persist as
 *   the SocialLinksJson string; any unknown keys already present are preserved.
 * - LayoutOptionsJson is carried through untouched so saving never wipes it.
 * - Logo/favicon can be typed as URLs or uploaded via the dedicated asset
 *   endpoints (which persist immediately and return the stored public URL).
 */
import { reactive, ref, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength, url, email, hexColor } from 'validators'
import { brandingApi } from 'modules/branding/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import ColorField from 'modules/branding/components/ColorField.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const notify = useNotify()

const SOCIAL_KEYS = ['facebook', 'instagram', 'twitter', 'linkedin', 'youtube']

const EMPTY = {
  brandName: '',
  domain: '',
  fontFamily: '',
  defaultLanguage: null,
  primaryColor: '',
  secondaryColor: '',
  accentColor: '',
  logoUrl: '',
  faviconUrl: '',
  supportEmail: '',
  supportPhone: '',
  facebook: '',
  instagram: '',
  twitter: '',
  linkedin: '',
  youtube: ''
}
const form = reactive({ ...EMPTY })

// Preserved as-is across edits so saving never wipes values the UI hides.
const extraSocial = ref({})
const layoutOptionsJson = ref(null)

const rules = {
  brandName: { required, maxLength: maxLength(200) },
  domain: { maxLength: maxLength(255) },
  primaryColor: { hexColor, maxLength: maxLength(32) },
  secondaryColor: { hexColor, maxLength: maxLength(32) },
  accentColor: { hexColor, maxLength: maxLength(32) },
  logoUrl: { url, maxLength: maxLength(500) },
  faviconUrl: { url, maxLength: maxLength(500) },
  supportEmail: { email },
  supportPhone: { maxLength: maxLength(50) },
  facebook: { url },
  instagram: { url },
  twitter: { url },
  linkedin: { url },
  youtube: { url }
}
const v$ = useVuelidate(rules, form)

const languageOptions = [
  { label: 'English', value: 'en' },
  { label: 'Spanish', value: 'es' },
  { label: 'French', value: 'fr' },
  { label: 'German', value: 'de' },
  { label: 'Portuguese', value: 'pt' },
  { label: 'Italian', value: 'it' },
  { label: 'Dutch', value: 'nl' },
  { label: 'Arabic', value: 'ar' },
  { label: 'Hindi', value: 'hi' },
  { label: 'Chinese (Simplified)', value: 'zh' },
  { label: 'Japanese', value: 'ja' }
]

const logoFile = ref(null)
const faviconFile = ref(null)
const uploadingLogo = ref(false)
const uploadingFavicon = ref(false)

function parseSocial (json) {
  const known = {}
  const extra = {}
  if (!json) return { known, extra }
  let obj = null
  try {
    obj = JSON.parse(json)
  } catch (e) {
    return { known, extra }
  }
  if (!obj || typeof obj !== 'object') return { known, extra }
  for (const [key, value] of Object.entries(obj)) {
    if (SOCIAL_KEYS.includes(key)) known[key] = value
    else extra[key] = value
  }
  return { known, extra }
}

watch(
  () => props.item,
  (item) => {
    const b = item || {}
    Object.assign(form, EMPTY, {
      brandName: b.brandName || '',
      domain: b.domain || '',
      fontFamily: b.fontFamily || '',
      defaultLanguage: b.defaultLanguage || null,
      primaryColor: b.primaryColor || '',
      secondaryColor: b.secondaryColor || '',
      accentColor: b.accentColor || '',
      logoUrl: b.logoUrl || '',
      faviconUrl: b.faviconUrl || '',
      supportEmail: b.supportEmail || '',
      supportPhone: b.supportPhone || ''
    })
    const { known, extra } = parseSocial(b.socialLinksJson)
    for (const key of SOCIAL_KEYS) form[key] = known[key] || ''
    extraSocial.value = extra
    layoutOptionsJson.value = b.layoutOptionsJson || null
    v$.value.$reset()
  },
  { immediate: true }
)

function toNull (value) {
  const s = (value ?? '').toString().trim()
  return s || null
}

function buildSocialJson () {
  const out = { ...extraSocial.value }
  for (const key of SOCIAL_KEYS) {
    const val = (form[key] || '').trim()
    if (val) out[key] = val
    else delete out[key]
  }
  return Object.keys(out).length ? JSON.stringify(out) : null
}

async function onPickLogo (file) {
  if (!file) return
  uploadingLogo.value = true
  try {
    const dto = await brandingApi.uploadLogo(file)
    if (dto && dto.logoUrl) form.logoUrl = dto.logoUrl
    notify.success('Logo uploaded')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    uploadingLogo.value = false
    logoFile.value = null
  }
}

async function onPickFavicon (file) {
  if (!file) return
  uploadingFavicon.value = true
  try {
    const dto = await brandingApi.uploadFavicon(file)
    if (dto && dto.faviconUrl) form.faviconUrl = dto.faviconUrl
    notify.success('Favicon uploaded')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    uploadingFavicon.value = false
    faviconFile.value = null
  }
}

function onFileRejected () {
  notify.error('Only image files up to 2 MB are allowed.')
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    brandName: form.brandName.trim(),
    domain: toNull(form.domain),
    logoUrl: toNull(form.logoUrl),
    faviconUrl: toNull(form.faviconUrl),
    primaryColor: toNull(form.primaryColor),
    secondaryColor: toNull(form.secondaryColor),
    accentColor: toNull(form.accentColor),
    fontFamily: toNull(form.fontFamily),
    supportEmail: toNull(form.supportEmail),
    supportPhone: toNull(form.supportPhone),
    socialLinksJson: buildSocialJson(),
    layoutOptionsJson: layoutOptionsJson.value || null,
    defaultLanguage: form.defaultLanguage || null
  })
}
</script>
