<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit shipping method' : 'New shipping method'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Standard Shipping" />
    <AppSelect v-model="form.methodType" label="Type" :options="shippingMethodTypeOptions" />

    <AppTextField v-if="form.methodType === 'FlatRate'" v-model="form.flatRate" label="Flat rate" type="number" step="0.01" placeholder="e.g. 5.00" />
    <AppTextField v-if="form.methodType === 'FreeShipping'" v-model="form.freeShippingThreshold" label="Free over" type="number" step="0.01" placeholder="Order total for free shipping" />

    <!-- Weight/price rate tiers -->
    <template v-if="isTiered">
      <div class="row items-center justify-between q-mt-sm q-mb-xs">
        <AppFieldLabel :label="form.methodType === 'WeightBased' ? 'Weight tiers (kg)' : 'Price tiers (order total)'" />
        <q-btn flat dense no-caps color="primary" icon="o_add" label="Add tier" @click="tiers.push({ upTo: null, rate: null })" />
      </div>
      <div v-for="(t, i) in tiers" :key="i" class="row items-center q-col-gutter-sm q-mb-xs no-wrap">
        <div class="col"><q-input v-model.number="t.upTo" dense outlined type="number" step="0.01" :label="form.methodType === 'WeightBased' ? 'Up to kg' : 'Up to total'" /></div>
        <div class="col"><q-input v-model.number="t.rate" dense outlined type="number" step="0.01" label="Rate" /></div>
        <div class="col-auto"><q-btn flat dense round size="sm" icon="o_delete" color="negative" @click="tiers.splice(i, 1)" /></div>
      </div>
      <div v-if="!tiers.length" class="text-grey-6 text-caption q-mb-sm">No tiers yet — add one or more brackets.</div>
    </template>

    <!-- Per-zone rates -->
    <q-separator class="q-my-md" />
    <AppFieldLabel label="Zone rates">
      <template #hint>Optional per-zone override rate (blank = method's default rate applies)</template>
    </AppFieldLabel>
    <div v-if="!zoneRates.length" class="text-grey-6 text-caption q-mb-sm">No shipping zones defined yet.</div>
    <div v-for="z in zoneRates" :key="z.shippingZoneId" class="row items-center q-col-gutter-sm q-mb-xs no-wrap">
      <div class="col text-body2">{{ z.name }}</div>
      <div class="col-4"><q-input v-model.number="z.rate" dense outlined type="number" step="0.01" placeholder="Rate" /></div>
    </div>

    <q-separator class="q-my-md" />
    <div class="row q-col-gutter-sm items-center">
      <div class="col-6"><AppTextField v-model="form.displayOrder" label="Display order" type="number" /></div>
      <div class="col-6"><q-toggle v-model="form.isEnabled" label="Enabled" color="primary" /></div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * ShippingMethodFormDrawer (WO-116): custom shipping method (flat / weight / price /
 * free), a per-bracket tier editor for weight/price methods, and per-zone override
 * rates (ShippingMethodZoneRate) populated from the defined shipping zones.
 */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { shippingMethodTypeOptions, shippingZoneApi } from 'modules/shipping/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({ modelValue: { type: Boolean, default: false }, item: { type: Object, default: null }, saving: { type: Boolean, default: false } })
const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])
const isEdit = computed(() => !!(props.item && props.item.id))
const isTiered = computed(() => form.methodType === 'WeightBased' || form.methodType === 'PriceBased')

const EMPTY = { name: '', methodType: 'FlatRate', flatRate: null, freeShippingThreshold: null, isEnabled: true, displayOrder: 0 }
const form = reactive({ ...EMPTY })
const tiers = ref([])
const zoneRates = ref([]) // [{ shippingZoneId, name, rate }]

const rules = { name: { required, maxLength: maxLength(200) } }
const v$ = useVuelidate(rules, form)

watch(() => props.item, (item) => {
  Object.assign(form, EMPTY, item ? {
    name: item.name, methodType: item.methodType, flatRate: item.flatRate,
    freeShippingThreshold: item.freeShippingThreshold, isEnabled: item.isEnabled, displayOrder: item.displayOrder
  } : {})
  tiers.value = parseTiers(item?.tiersJson)
  syncZoneRates(item?.zoneRates || [])
  v$.value.$reset()
}, { immediate: true })

function parseTiers (json) {
  try { const a = json ? JSON.parse(json) : []; return Array.isArray(a) ? a.map((t) => ({ upTo: t.upTo ?? null, rate: t.rate ?? null })) : [] } catch (e) { return [] }
}

const allZones = ref([])
async function loadZones () {
  try {
    const r = await shippingZoneApi.list({ page: 1, pageSize: 200 })
    allZones.value = Array.isArray(r) ? r : r?.items || []
  } catch (e) { allZones.value = [] }
  syncZoneRates(props.item?.zoneRates || [])
}

// Merge the loaded zones with any existing per-zone rates on the method.
function syncZoneRates (existing) {
  const byZone = {}
  for (const zr of existing) byZone[zr.shippingZoneId] = zr.rate
  zoneRates.value = allZones.value.map((z) => ({ shippingZoneId: z.id, name: z.name, rate: byZone[z.id] ?? null }))
}

function num (v) { if (v === '' || v == null) return null; const n = Number(v); return Number.isFinite(n) ? n : null }

async function onSubmit () {
  const ok = await v$.value.$validate(); if (!ok) return
  const cleanTiers = tiers.value.filter((t) => t.upTo != null && t.rate != null).map((t) => ({ upTo: Number(t.upTo), rate: Number(t.rate) }))
  emit('submit', {
    name: form.name.trim(),
    methodType: form.methodType,
    flatRate: form.methodType === 'FlatRate' ? num(form.flatRate) : null,
    freeShippingThreshold: form.methodType === 'FreeShipping' ? num(form.freeShippingThreshold) : null,
    tiersJson: isTiered.value && cleanTiers.length ? JSON.stringify(cleanTiers) : null,
    isEnabled: form.isEnabled,
    displayOrder: num(form.displayOrder) || 0,
    zoneRates: zoneRates.value.filter((z) => z.rate != null && z.rate !== '').map((z) => ({ shippingZoneId: z.shippingZoneId, rate: Number(z.rate) }))
  })
}

onMounted(loadZones)
</script>
