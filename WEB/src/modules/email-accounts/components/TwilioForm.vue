<template>
  <q-card flat bordered class="twilio-card relative-position">
    <q-card-section>
      <div class="row items-center q-mb-sm">
        <div class="col">
          <div class="text-subtitle1">Twilio SMS</div>
          <div class="text-caption text-grey-7">Credentials for outbound SMS / WhatsApp notifications.</div>
        </div>
        <q-badge
          :color="config && config.isConfigured ? 'positive' : 'grey'"
          :label="config && config.isConfigured ? 'Configured' : 'Not configured'"
        />
      </div>

      <q-inner-loading :showing="loading">
        <q-spinner color="primary" size="32px" />
      </q-inner-loading>

      <AppTextField
        v-model="form.accountSid"
        label="Account SID"
        required
        :v="v$.accountSid"
        autocomplete="off"
        placeholder="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
      />

      <AppTextField
        v-model="form.authToken"
        label="Auth token"
        required
        :v="v$.authToken"
        :type="showToken ? 'text' : 'password'"
        autocomplete="new-password"
      >
        <template #hint>
          <span v-if="config && config.isConfigured" class="text-caption text-grey-7">
            Stored token {{ config.maskedToken || '••••' }} — re-enter it to save any change.
          </span>
          <span v-else class="text-caption text-grey-7">The auth token is stored encrypted and never shown again.</span>
        </template>
        <template #append>
          <q-icon
            :name="showToken ? 'o_visibility_off' : 'o_visibility'"
            class="cursor-pointer"
            @click="showToken = !showToken"
          />
        </template>
      </AppTextField>

      <AppTextField
        v-model="form.fromNumber"
        label="From number"
        required
        :v="v$.fromNumber"
        placeholder="+15005550006"
      />

      <q-toggle v-model="form.enabled" label="Enabled" color="primary" />
    </q-card-section>

    <q-separator />

    <q-card-actions align="right" class="q-pa-md">
      <q-btn
        flat
        no-caps
        color="negative"
        icon="o_delete"
        label="Delete"
        :disable="!canWrite || !(config && config.isConfigured)"
        @click="$emit('delete')"
      />
      <q-space />
      <q-btn
        flat
        no-caps
        color="primary"
        icon="o_send"
        label="Test"
        :loading="testing"
        :disable="!(config && config.isConfigured)"
        @click="$emit('test')"
      />
      <q-btn
        unelevated
        no-caps
        color="primary"
        icon="o_save"
        label="Save"
        :loading="saving"
        :disable="!canWrite"
        @click="onSubmit"
      />
    </q-card-actions>
  </q-card>
</template>

<script setup>
/*
 * TwilioForm (WO-77): presentational form for the Twilio credential. It edits the
 * three fields (Account SID / Auth token / From number) plus an enabled flag and
 * emits `submit` with the raw fields; the page maps them onto the credential
 * vault (token = encrypted value, the rest = description JSON). The auth token is
 * write-only, so the API requires it to be (re-)entered on every save.
 */
import { reactive, ref, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, phone } from 'validators'

const props = defineProps({
  config: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  saving: { type: Boolean, default: false },
  testing: { type: Boolean, default: false },
  canWrite: { type: Boolean, default: true }
})

const emit = defineEmits(['submit', 'test', 'delete'])

const EMPTY = { accountSid: '', authToken: '', fromNumber: '', enabled: true }
const form = reactive({ ...EMPTY })
const showToken = ref(false)

const rules = {
  accountSid: { required },
  authToken: { required },
  fromNumber: { required, phone: phone() }
}
const v$ = useVuelidate(rules, form)

watch(
  () => props.config,
  (config) => {
    Object.assign(form, EMPTY, {
      accountSid: config?.accountSid || '',
      authToken: '',
      fromNumber: config?.fromNumber || '',
      enabled: config?.enabled ?? true
    })
    showToken.value = false
    v$.value.$reset()
  },
  { immediate: true }
)

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    accountSid: form.accountSid.trim(),
    authToken: form.authToken,
    fromNumber: form.fromNumber.trim(),
    enabled: !!form.enabled
  })
}
</script>

<style scoped>
.twilio-card {
  max-width: 620px;
}
</style>
