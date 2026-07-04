<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit store' : 'New store'"
    :saving="saving"
    :default-width="560"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Downtown Store" />

    <div class="text-caption text-grey-7 q-mt-sm q-mb-xs">Address</div>
    <AppTextField v-model="form.addressLine1" label="Address line 1" placeholder="Street address" />
    <AppTextField v-model="form.addressLine2" label="Address line 2" placeholder="Suite, unit (optional)" />
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.city" label="City" /></div>
      <div class="col-6"><AppTextField v-model="form.stateProvince" label="State / Province" /></div>
    </div>
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.postalCode" label="Postal code" /></div>
      <div class="col-6"><AppTextField v-model="form.countryCode" label="Country (ISO-2)" placeholder="US" maxlength="2" /></div>
    </div>
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.latitude" label="Latitude" type="number" step="0.000001" placeholder="Used for order routing" /></div>
      <div class="col-6"><AppTextField v-model="form.longitude" label="Longitude" type="number" step="0.000001" /></div>
    </div>

    <div class="text-caption text-grey-7 q-mt-sm q-mb-xs">Contact & locale</div>
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.contactEmail" label="Contact email" /></div>
      <div class="col-6"><AppTextField v-model="form.contactPhone" label="Contact phone" /></div>
    </div>
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.timeZone" label="Time zone" placeholder="e.g. America/New_York" /></div>
      <div class="col-6"><AppTextField v-model="form.currencyDisplay" label="Display currency" placeholder="e.g. USD" /></div>
    </div>
    <AppTextField v-model="form.orderCapacityLimit" label="Order capacity limit" type="number" placeholder="Max concurrent orders (blank = unlimited)" />

    <div class="text-caption text-grey-7 q-mt-md q-mb-xs">Operating hours</div>
    <div v-for="row in hours" :key="row.day" class="row items-center q-col-gutter-sm q-mb-xs">
      <div class="col-4 text-body2">{{ row.day }}</div>
      <div class="col-auto"><q-toggle v-model="row.closed" label="Closed" dense /></div>
      <template v-if="!row.closed">
        <div class="col"><q-input v-model="row.open" dense outlined type="time" /></div>
        <div class="col"><q-input v-model="row.close" dense outlined type="time" /></div>
      </template>
    </div>

    <q-separator class="q-my-md" />
    <div class="column q-gutter-xs">
      <q-toggle v-model="form.isEnabled" label="Enabled" color="primary" />
      <q-toggle v-model="form.maintenanceMode" label="Maintenance mode" color="orange" />
      <q-toggle v-model="form.guestOrderingEnabled" label="Guest ordering allowed" color="primary" />
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * StoreFormDrawer (WO-113): full store configuration — profile, geo-coordinates,
 * contact, locale, order capacity, a per-day operating-hours grid, and the
 * enabled / maintenance / guest-ordering toggles.
 */
import { reactive, ref, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { WEEK_DAYS } from 'modules/stores/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = {
  name: '', addressLine1: '', addressLine2: '', city: '', stateProvince: '', postalCode: '', countryCode: '',
  latitude: null, longitude: null, contactEmail: '', contactPhone: '', timeZone: 'UTC', currencyDisplay: '',
  orderCapacityLimit: null, isEnabled: true, maintenanceMode: false, guestOrderingEnabled: true
}
const form = reactive({ ...EMPTY })
const hours = ref(WEEK_DAYS.map((day) => ({ day, closed: false, open: '09:00', close: '17:00' })))

const rules = { name: { required, maxLength: maxLength(200) } }
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item || {})
    hours.value = parseHours(item?.operatingHoursJson)
    v$.value.$reset()
  },
  { immediate: true }
)

function parseHours (json) {
  let parsed = {}
  try { parsed = json ? JSON.parse(json) : {} } catch (e) { parsed = {} }
  return WEEK_DAYS.map((day) => {
    const d = parsed[day] || {}
    return { day, closed: d.closed === true, open: d.open || '09:00', close: d.close || '17:00' }
  })
}

function serializeHours () {
  const out = {}
  for (const r of hours.value) out[r.day] = r.closed ? { closed: true } : { open: r.open, close: r.close }
  return JSON.stringify(out)
}

function numberOrNull (v) {
  if (v === '' || v === null || v === undefined) return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    name: form.name.trim(),
    addressLine1: form.addressLine1 || null,
    addressLine2: form.addressLine2 || null,
    city: form.city || null,
    stateProvince: form.stateProvince || null,
    postalCode: form.postalCode || null,
    countryCode: form.countryCode ? form.countryCode.toUpperCase() : null,
    latitude: numberOrNull(form.latitude),
    longitude: numberOrNull(form.longitude),
    contactEmail: form.contactEmail || null,
    contactPhone: form.contactPhone || null,
    timeZone: form.timeZone || 'UTC',
    currencyDisplay: form.currencyDisplay || null,
    orderCapacityLimit: numberOrNull(form.orderCapacityLimit),
    operatingHoursJson: serializeHours(),
    isEnabled: form.isEnabled,
    maintenanceMode: form.maintenanceMode,
    guestOrderingEnabled: form.guestOrderingEnabled
  })
}
</script>
