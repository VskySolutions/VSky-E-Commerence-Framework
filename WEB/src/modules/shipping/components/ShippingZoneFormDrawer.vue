<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit shipping zone' : 'New shipping zone'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. US East Coast" />
    <AppTextField v-model="form.countryCode" label="Country (ISO-2)" required :v="v$.countryCode" placeholder="US" maxlength="2" />
    <AppTextField v-model="form.region" label="Region / State" placeholder="Optional" />
    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppTextField v-model="form.postalCodeStart" label="Postal from" placeholder="Optional" /></div>
      <div class="col-6"><AppTextField v-model="form.postalCodeEnd" label="Postal to" placeholder="Optional" /></div>
    </div>
    <q-toggle v-model="form.isEnabled" label="Enabled" color="primary" class="q-mt-sm" />
  </AppFormDrawer>
</template>

<script setup>
/* ShippingZoneFormDrawer (WO-116): a shipping zone by country / region / postal range. */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'

const props = defineProps({ modelValue: { type: Boolean, default: false }, item: { type: Object, default: null }, saving: { type: Boolean, default: false } })
const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])
const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = { name: '', countryCode: '', region: '', postalCodeStart: '', postalCodeEnd: '', isEnabled: true }
const form = reactive({ ...EMPTY })
const rules = { name: { required, maxLength: maxLength(200) }, countryCode: { required } }
const v$ = useVuelidate(rules, form)

watch(() => props.item, (item) => { Object.assign(form, EMPTY, item || {}); v$.value.$reset() }, { immediate: true })

async function onSubmit () {
  const ok = await v$.value.$validate(); if (!ok) return
  emit('submit', {
    name: form.name.trim(),
    countryCode: form.countryCode.toUpperCase(),
    region: form.region || null,
    postalCodeStart: form.postalCodeStart || null,
    postalCodeEnd: form.postalCodeEnd || null,
    isEnabled: form.isEnabled
  })
}
</script>
