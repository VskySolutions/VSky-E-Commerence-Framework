<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit credential' : 'New credential'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <template v-if="isEdit">
      <AppTextField :model-value="form.serviceType" label="Service type" disable />
    </template>
    <template v-else>
      <AppSelect
        v-model="form.serviceChoice"
        label="Service type"
        required
        :options="serviceOptions"
        :v="v$.serviceChoice"
      />
      <AppTextField
        v-if="form.serviceChoice === CUSTOM"
        v-model="form.customServiceType"
        label="Custom service type"
        required
        :v="v$.customServiceType"
        placeholder="e.g. mailchimp"
        maxlength="100"
      />
    </template>

    <AppTextField
      v-model="form.secret"
      label="Secret value"
      required
      :v="v$.secret"
      :type="showValue ? 'text' : 'password'"
      autocomplete="new-password"
    >
      <template #hint>
        <span v-if="isEdit" class="text-caption text-grey-7">Replaces the stored secret</span>
      </template>
      <template #append>
        <q-icon
          :name="showValue ? 'o_visibility_off' : 'o_visibility'"
          class="cursor-pointer"
          @click="showValue = !showValue"
        />
      </template>
    </AppTextField>

    <AppTextField
      v-model="form.description"
      label="Description"
      type="textarea"
      autogrow
      maxlength="500"
      :v="v$.description"
    />
  </AppFormDrawer>
</template>

<script setup>
/*
 * CredentialFormDrawer (WO-9 REQ-TEN-002): a Vuelidate-validated create/edit
 * form inside AppFormDrawer. Credentials are keyed by serviceType so both create
 * and edit resolve to PUT /api/tenant/credentials/{serviceType}. The secret is
 * write-only (never returned), so it must be (re-)entered to save. Emits
 * `submit` with { serviceType, value, description }.
 */
import { reactive, ref, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, requiredIf, maxLength } from 'validators'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const CUSTOM = '__custom__'

const isEdit = computed(() => !!(props.item && props.item.serviceType))

const serviceOptions = [
  { label: 'Stripe', value: 'stripe' },
  { label: 'Stripe Tax', value: 'stripe-tax' },
  { label: 'PayPal', value: 'paypal' },
  { label: 'Razorpay', value: 'razorpay' },
  { label: 'Square', value: 'square' },
  { label: 'Authorize.Net', value: 'authorizenet' },
  { label: 'TaxJar', value: 'taxjar' },
  { label: 'DHL', value: 'dhl' },
  { label: 'UPS', value: 'ups' },
  { label: 'FedEx', value: 'fedex' },
  { label: 'USPS', value: 'usps' },
  { label: 'SMTP', value: 'smtp' },
  { label: 'Azure Blob Storage', value: 'azure-blob' },
  { label: 'Other…', value: CUSTOM }
]

const EMPTY = { serviceType: '', serviceChoice: '', customServiceType: '', secret: '', description: '' }
const form = reactive({ ...EMPTY })
const showValue = ref(false)

const rules = {
  serviceChoice: { requiredIf: requiredIf(() => !isEdit.value) },
  customServiceType: {
    requiredIf: requiredIf(() => !isEdit.value && form.serviceChoice === CUSTOM),
    maxLength: maxLength(100)
  },
  secret: { required },
  description: { maxLength: maxLength(500) }
}
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, {
      serviceType: item?.serviceType || '',
      description: item?.description || ''
    })
    showValue.value = false
    v$.value.$reset()
  },
  { immediate: true }
)

function resolvedServiceType () {
  if (isEdit.value) return form.serviceType
  return form.serviceChoice === CUSTOM ? form.customServiceType.trim() : form.serviceChoice
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  const description = (form.description || '').trim()
  emit('submit', {
    serviceType: resolvedServiceType(),
    value: form.secret,
    description: description || null
  })
}
</script>
