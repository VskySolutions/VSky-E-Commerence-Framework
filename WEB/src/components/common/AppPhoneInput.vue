<template>
  <div class="app-field">
    <AppFieldLabel v-if="label" :label="label" :required="required" />

    <q-input
      dense
      outlined
      :model-value="national"
      :error="hasError"
      :error-message="errorMessage"
      hide-bottom-space
      inputmode="tel"
      v-bind="$attrs"
      @update:model-value="onInput"
      @blur="onBlur"
    >
      <template #prepend>
        <q-select
          v-model="country"
          :options="countryOptions"
          dense
          borderless
          emit-value
          map-options
          options-dense
          style="min-width: 92px"
          @update:model-value="emitValue"
        />
      </template>
    </q-input>
  </div>
</template>

<script setup>
/*
 * AppPhoneInput (WO-94 Step 10): country calling-code selector + national
 * number field, powered by libphonenumber-js. Emits an E.164 string when the
 * number is parseable, otherwise the raw national digits.
 */
import { ref, computed, watch } from 'vue'
import {
  getCountries,
  getCountryCallingCode,
  parsePhoneNumberFromString,
  AsYouType,
  isValidPhoneNumber
} from 'libphonenumber-js'
import AppFieldLabel from './AppFieldLabel.vue'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, default: '' },
  required: { type: Boolean, default: false },
  defaultCountry: { type: String, default: 'US' },
  v: { type: Object, default: null }
})

const emit = defineEmits(['update:modelValue'])

const countryOptions = getCountries().map((c) => ({
  label: `${c} +${getCountryCallingCode(c)}`,
  value: c
}))

const country = ref(props.defaultCountry || 'US')
const national = ref('')

// Seed from an incoming E.164 value.
function hydrate (value) {
  if (!value) {
    national.value = ''
    return
  }
  const parsed = parsePhoneNumberFromString(String(value))
  if (parsed) {
    if (parsed.country) country.value = parsed.country
    national.value = parsed.formatNational()
  } else {
    national.value = String(value)
  }
}
hydrate(props.modelValue)

watch(
  () => props.modelValue,
  (val) => {
    // Avoid clobbering while the user is mid-edit.
    const current = parsePhoneNumberFromString(national.value || '', country.value)
    if (!current || current.number !== val) hydrate(val)
  }
)

function onInput (val) {
  const formatter = new AsYouType(country.value)
  national.value = formatter.input(String(val || ''))
  emitValue()
}

function emitValue () {
  const parsed = parsePhoneNumberFromString(national.value || '', country.value)
  emit('update:modelValue', parsed ? parsed.number : national.value)
}

const hasError = computed(() => {
  if (props.v && props.v.$error) return true
  return false
})
const errorMessage = computed(() => {
  if (props.v && Array.isArray(props.v.$errors) && props.v.$errors.length) {
    return props.v.$errors[0].$message
  }
  return undefined
})

function onBlur () {
  if (props.v && typeof props.v.$touch === 'function') props.v.$touch()
}

// Exposed for callers that want to validate outside Vuelidate.
defineExpose({
  isValid: () => isValidPhoneNumber(national.value || '', country.value)
})
</script>
