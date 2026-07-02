<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit SMTP account' : 'New SMTP account'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField
      v-model="form.displayName"
      label="Display name"
      required
      :v="v$.displayName"
      maxlength="200"
      placeholder="e.g. Primary transactional"
    />

    <div class="row q-col-gutter-sm">
      <div class="col-8">
        <AppTextField v-model="form.host" label="Host" required :v="v$.host" maxlength="255" placeholder="smtp.example.com" />
      </div>
      <div class="col-4">
        <AppTextField v-model="form.port" label="Port" required :v="v$.port" type="number" placeholder="587" />
      </div>
    </div>

    <div class="row q-col-gutter-sm">
      <div class="col-6">
        <AppSelect v-model="form.encryptionMode" label="Encryption" :options="encryptionOptions" />
      </div>
      <div class="col-6">
        <AppSelect v-model="form.authMethod" label="Auth method" :options="authMethodOptions" />
      </div>
    </div>

    <AppTextField v-model="form.username" label="Username" :v="v$.username" maxlength="255" autocomplete="off" />

    <AppTextField
      v-model="form.password"
      label="Password"
      :type="showPassword ? 'text' : 'password'"
      autocomplete="new-password"
    >
      <template #hint>
        <span v-if="isEdit && item && item.hasPassword" class="text-caption text-grey-7">
          A password is stored — leave blank to keep it.
        </span>
        <span v-else class="text-caption text-grey-7">Optional; required only if your server needs authentication.</span>
      </template>
      <template #append>
        <q-icon
          :name="showPassword ? 'o_visibility_off' : 'o_visibility'"
          class="cursor-pointer"
          @click="showPassword = !showPassword"
        />
      </template>
    </AppTextField>

    <AppTextField v-model="form.fromName" label="From name" required :v="v$.fromName" maxlength="200" placeholder="Acme Store" />
    <AppTextField v-model="form.fromEmail" label="From email" required :v="v$.fromEmail" maxlength="255" placeholder="noreply@example.com" />

    <AppSelect
      v-model="form.category"
      label="Notification category"
      :options="categoryOptions"
    />
    <div class="text-caption text-grey-7 q-mb-sm">
      Only one enabled account is used per category. Enabling this account disables any other enabled account in the same category.
    </div>

    <q-toggle v-model="form.enabled" label="Enabled" color="primary" />
  </AppFormDrawer>
</template>

<script setup>
/*
 * SmtpAccountFormDrawer (WO-77): a Vuelidate-validated create/edit form for a
 * tenant SMTP account, inside AppFormDrawer. The password is write-only (never
 * returned) so it is only sent when (re-)entered. Enum values are the API's
 * exact member-name strings. Emits `submit` with the ready-to-POST/PUT payload.
 */
import { reactive, ref, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength, minValue, maxValue, integer, email } from 'validators'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const encryptionOptions = [
  { label: 'None', value: 'None' },
  { label: 'SSL', value: 'Ssl' },
  { label: 'TLS', value: 'Tls' },
  { label: 'STARTTLS', value: 'StartTls' }
]

const authMethodOptions = [
  { label: 'Auto', value: 'Auto' },
  { label: 'LOGIN', value: 'Login' },
  { label: 'PLAIN', value: 'Plain' },
  { label: 'CRAM-MD5', value: 'CramMd5' },
  { label: 'OAuth2', value: 'OAuth2' }
]

const categoryOptions = [
  { label: 'Unassigned', value: null },
  { label: 'Transactional', value: 'Transactional' },
  { label: 'Marketing', value: 'Marketing' }
]

const EMPTY = {
  displayName: '',
  host: '',
  port: 587,
  username: '',
  password: '',
  fromName: '',
  fromEmail: '',
  encryptionMode: 'StartTls',
  authMethod: 'Auto',
  category: null,
  enabled: true
}
const form = reactive({ ...EMPTY })
const showPassword = ref(false)

const rules = {
  displayName: { required, maxLength: maxLength(200) },
  host: { required, maxLength: maxLength(255) },
  port: { required, integer, minValue: minValue(1), maxValue: maxValue(65535) },
  username: { maxLength: maxLength(255) },
  fromName: { required, maxLength: maxLength(200) },
  fromEmail: { required, email, maxLength: maxLength(255) }
}
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, {
      displayName: item?.displayName || '',
      host: item?.host || '',
      port: item?.port ?? 587,
      username: item?.username || '',
      password: '',
      fromName: item?.fromName || '',
      fromEmail: item?.fromEmail || '',
      encryptionMode: item?.encryptionMode || 'StartTls',
      authMethod: item?.authMethod || 'Auto',
      category: item?.category ?? null,
      enabled: item?.enabled ?? true
    })
    showPassword.value = false
    v$.value.$reset()
  },
  { immediate: true }
)

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    displayName: form.displayName.trim(),
    host: form.host.trim(),
    port: Number(form.port),
    username: (form.username || '').trim() || null,
    // Blank password => null: on update the API keeps the stored one; on create it means "no auth".
    password: form.password ? form.password : null,
    fromName: form.fromName.trim(),
    fromEmail: form.fromEmail.trim(),
    encryptionMode: form.encryptionMode,
    authMethod: form.authMethod,
    category: form.category || null,
    enabled: !!form.enabled
  })
}
</script>
