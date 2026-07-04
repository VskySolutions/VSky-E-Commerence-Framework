<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit coupon' : 'New coupon'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.code" label="Code" required :v="v$.code" placeholder="e.g. SUMMER10" />
    <AppSelect v-model="form.discountId" label="Linked discount" required :v="v$.discountId" :options="discountOptions" placeholder="Select the discount this code applies" />

    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppSelect v-model="form.usageType" label="Usage" :options="couponUsageOptions" /></div>
      <div class="col-6"><AppTextField v-if="form.usageType === 'Limited'" v-model="form.maxRedemptions" label="Max redemptions" type="number" placeholder="e.g. 100" /></div>
    </div>

    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppFieldLabel label="Starts" /><q-input v-model="form.startDate" dense outlined type="date" /></div>
      <div class="col-6"><AppFieldLabel label="Ends" /><q-input v-model="form.endDate" dense outlined type="date" /></div>
    </div>

    <q-toggle v-model="form.isActive" label="Active" color="primary" class="q-mt-sm" />
  </AppFormDrawer>
</template>

<script setup>
/* CouponFormDrawer (WO-115): create/edit a coupon code linked to a discount, with usage policy + window. */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { couponUsageOptions, discountApi } from 'modules/pricing/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})
const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = { code: '', discountId: null, usageType: 'SingleUse', maxRedemptions: null, startDate: '', endDate: '', isActive: true }
const form = reactive({ ...EMPTY })

const rules = { code: { required, maxLength: maxLength(64) }, discountId: { required } }
const v$ = useVuelidate(rules, form)

const discountOptions = ref([])

watch(() => props.item, (item) => {
  Object.assign(form, EMPTY)
  if (item) {
    Object.assign(form, item)
    form.startDate = toDateInput(item.startDateUtc)
    form.endDate = toDateInput(item.endDateUtc)
  }
  v$.value.$reset()
}, { immediate: true })

function toDateInput (v) { return v ? new Date(v).toISOString().slice(0, 10) : '' }

async function loadDiscounts () {
  try {
    const res = await discountApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(res) ? res : res?.items || []
    discountOptions.value = items.map((d) => ({ label: `${d.name} (${d.type === 'Percentage' ? d.value + '%' : d.value})`, value: d.id }))
  } catch (e) { discountOptions.value = [] }
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    code: form.code.trim(),
    discountId: form.discountId,
    usageType: form.usageType,
    maxRedemptions: form.usageType === 'Limited' ? (Number(form.maxRedemptions) || null) : null,
    startDateUtc: form.startDate || null,
    endDateUtc: form.endDate || null,
    isActive: form.isActive
  })
}

onMounted(loadDiscounts)
</script>
