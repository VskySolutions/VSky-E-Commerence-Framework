<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? `Edit ${form.currencyCode}` : 'New currency'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField
      v-model="form.currencyCode"
      label="Currency code"
      required
      :v="v$.currencyCode"
      :disable="isEdit"
      placeholder="USD"
      maxlength="3"
      @update:model-value="onCodeInput"
    >
      <template #hint><span class="text-caption text-grey-6">ISO 4217</span></template>
    </AppTextField>
    <AppTextField
      v-model="form.symbol"
      label="Symbol"
      required
      :v="v$.symbol"
      placeholder="$"
      maxlength="8"
    />
    <AppTextField
      v-model="form.exchangeRate"
      label="Exchange rate"
      required
      :v="v$.exchangeRate"
      type="number"
      step="any"
      min="0"
      :disable="isBase"
    >
      <template #hint>
        <span class="text-caption text-grey-6">{{ isBase ? 'Fixed at 1' : 'Per 1 base unit' }}</span>
      </template>
    </AppTextField>

    <q-separator class="q-my-md" />

    <div class="q-mb-sm">
      <q-toggle v-model="form.isEnabled" label="Enabled" color="primary" :disable="isBase" />
      <div v-if="isBase" class="text-caption text-grey-7 q-ml-sm">
        The base currency is always enabled.
      </div>
    </div>
    <div>
      <q-toggle v-model="form.isRateLocked" label="Lock rate (skip auto-refresh)" color="primary" :disable="isBase" />
      <div v-if="isBase" class="text-caption text-grey-7 q-ml-sm">
        The base currency rate is always locked.
      </div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * CurrencyFormDrawer (WO-91 REQ-TEN-006): a Vuelidate-validated create/edit form
 * inside AppFormDrawer. Emits `submit` with a payload shaped for both the
 * CreateCurrencyCommand and the UpdateCurrencyCommand; the page picks the fields
 * it needs. The currency code is the key, so it is read-only when editing.
 *
 * The base currency is the numéraire: its rate is pinned to 1 and always locked
 * server-side, so those inputs are disabled here to reflect that.
 */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength, helpers } from 'validators'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.currencyCode))
const isBase = computed(() => !!(props.item && props.item.isBaseCurrency))

const EMPTY = { currencyCode: '', symbol: '', exchangeRate: 1, isEnabled: true, isRateLocked: false }
const form = reactive({ ...EMPTY })

const isoCode = helpers.withMessage(
  'Use a three-letter code (e.g. USD)',
  (v) => !helpers.req(v) || /^[A-Za-z]{3}$/.test(String(v))
)
const positiveRate = helpers.withMessage(
  'Enter a rate greater than 0',
  (v) => !helpers.req(v) || (Number.isFinite(Number(v)) && Number(v) > 0)
)

const rules = {
  currencyCode: { required, isoCode },
  symbol: { required, maxLength: maxLength(8) },
  exchangeRate: { required, positiveRate }
}
const v$ = useVuelidate(rules, form)

function onCodeInput (val) {
  form.currencyCode = (val || '').toUpperCase().slice(0, 3)
}

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item ? {
      currencyCode: item.currencyCode || '',
      symbol: item.symbol || '',
      exchangeRate: item.exchangeRate ?? 1,
      isEnabled: item.isEnabled ?? true,
      isRateLocked: item.isRateLocked ?? false
    } : {})
    v$.value.$reset()
  },
  { immediate: true }
)

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    currencyCode: form.currencyCode.trim().toUpperCase(),
    symbol: form.symbol.trim(),
    exchangeRate: Number(form.exchangeRate),
    isEnabled: !!form.isEnabled,
    isRateLocked: !!form.isRateLocked
  })
}
</script>
