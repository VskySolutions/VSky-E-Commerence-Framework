<template>
  <div class="app-field">
    <AppFieldLabel v-if="label" :label="label" :required="required" />

    <q-input
      dense
      outlined
      :model-value="national"
      :error="hasError"
      :error-message="errorMessage"
      :placeholder="placeholder"
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
          style="min-width: 96px"
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
  getExampleNumber,
  AsYouType,
  isPossiblePhoneNumber
} from 'libphonenumber-js'
import examples from 'libphonenumber-js/examples.mobile.json'
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

// Calling-code picker with the common markets (US, India) pinned to the top, then the rest.
const PINNED = ['US', 'IN']
const countryOptions = (() => {
  const all = getCountries()
  const opt = (c) => ({ label: `${c} +${getCountryCallingCode(c)}`, value: c })
  const pinned = PINNED.filter((c) => all.includes(c)).map(opt)
  const rest = all.filter((c) => !PINNED.includes(c)).sort().map(opt)
  return [...pinned, ...rest]
})()

const country = ref(props.defaultCountry || 'US')
const national = ref('')

// A real example number for the selected country, shown as the placeholder (formatted nationally).
const placeholder = computed(() => {
  try {
    const ex = getExampleNumber(country.value, examples)
    return ex ? ex.formatNational() : ''
  } catch (e) {
    return ''
  }
})

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

// Follow the address country while the number is still empty (selecting a country sets the code).
watch(
  () => props.defaultCountry,
  (c) => {
    if (c && !national.value) country.value = c
  }
)

const MAX_E164_DIGITS = 15

function onInput (val) {
  let s = String(val || '')
  const digits = s.replace(/\D/g, '')
  // E.164 permits at most 15 digits — rebuild from the capped digits so the field can't accept more.
  if (digits.length > MAX_E164_DIGITS) s = digits.slice(0, MAX_E164_DIGITS)
  const formatter = new AsYouType(country.value)
  national.value = formatter.input(s)
  emitValue()
}

function emitValue () {
  const parsed = parsePhoneNumberFromString(national.value || '', country.value)
  emit('update:modelValue', parsed ? parsed.number : national.value)
}

// Self-validation (shown once the field is blurred): flags a missing required number and, more
// importantly, an invalid/too-long number that libphonenumber can't accept for the chosen country.
const touched = ref(false)
const internalError = computed(() => {
  const raw = national.value || ''
  if (!raw.trim()) return props.required ? 'Required' : ''
  // isPossible checks the number's length/shape for the country (catches too-few/too-many digits)
  // without rejecting plausible numbers whose exact prefix isn't in the assigned numbering plan.
  return isPossiblePhoneNumber(raw, country.value) ? '' : 'Enter a valid phone number'
})

const hasError = computed(() => {
  if (props.v && props.v.$error) return true
  return touched.value && !!internalError.value
})
const errorMessage = computed(() => {
  if (props.v && Array.isArray(props.v.$errors) && props.v.$errors.length) {
    return props.v.$errors[0].$message
  }
  return touched.value && internalError.value ? internalError.value : undefined
})

function onBlur () {
  touched.value = true
  if (props.v && typeof props.v.$touch === 'function') props.v.$touch()
}

// Exposed for callers that want to validate outside Vuelidate.
defineExpose({
  isValid: () => isPossiblePhoneNumber(national.value || '', country.value)
})
</script>
