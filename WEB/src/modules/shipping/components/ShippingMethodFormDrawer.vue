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
    <template v-if="form.methodType === 'WeightBased' || form.methodType === 'PriceBased'">
      <AppFieldLabel label="Rate tiers (JSON)">
        <template #hint>{{ form.methodType === 'WeightBased' ? 'e.g. [{"upTo":1,"rate":5},{"upTo":5,"rate":10}] by kg' : 'e.g. [{"upTo":50,"rate":8},{"upTo":100,"rate":5}] by order total' }}</template>
      </AppFieldLabel>
      <q-input v-model="form.tiersJson" dense outlined type="textarea" autogrow placeholder="[]" />
    </template>

    <div class="row q-col-gutter-sm items-center q-mt-sm">
      <div class="col-6"><AppTextField v-model="form.displayOrder" label="Display order" type="number" /></div>
      <div class="col-6"><q-toggle v-model="form.isEnabled" label="Enabled" color="primary" /></div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/* ShippingMethodFormDrawer (WO-116): custom shipping method (flat / weight / price / free). */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { shippingMethodTypeOptions } from 'modules/shipping/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({ modelValue: { type: Boolean, default: false }, item: { type: Object, default: null }, saving: { type: Boolean, default: false } })
const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])
const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = { name: '', methodType: 'FlatRate', flatRate: null, freeShippingThreshold: null, tiersJson: '', isEnabled: true, displayOrder: 0 }
const form = reactive({ ...EMPTY })
const rules = { name: { required, maxLength: maxLength(200) } }
const v$ = useVuelidate(rules, form)

watch(() => props.item, (item) => { Object.assign(form, EMPTY, item || {}); v$.value.$reset() }, { immediate: true })
function num (v) { if (v === '' || v == null) return null; const n = Number(v); return Number.isFinite(n) ? n : null }

async function onSubmit () {
  const ok = await v$.value.$validate(); if (!ok) return
  emit('submit', {
    name: form.name.trim(),
    methodType: form.methodType,
    flatRate: form.methodType === 'FlatRate' ? num(form.flatRate) : null,
    freeShippingThreshold: form.methodType === 'FreeShipping' ? num(form.freeShippingThreshold) : null,
    tiersJson: (form.methodType === 'WeightBased' || form.methodType === 'PriceBased') ? (form.tiersJson || null) : null,
    isEnabled: form.isEnabled,
    displayOrder: num(form.displayOrder) || 0
  })
}
</script>
