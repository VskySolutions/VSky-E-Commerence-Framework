<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit discount' : 'New discount'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Summer Sale 10%" />

    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppSelect v-model="form.scope" label="Scope" :options="discountScopeOptions" /></div>
      <div class="col-6"><AppSelect v-model="form.type" label="Type" :options="discountTypeOptions" /></div>
    </div>

    <AppTextField
      v-model="form.value"
      :label="form.type === 'Percentage' ? 'Value (%)' : 'Value (amount)'"
      type="number"
      step="0.01"
      required
      :v="v$.value"
      placeholder="0"
    />

    <AppSelect
      v-if="form.scope === 'Product'"
      v-model="form.productId"
      label="Product"
      clearable
      :options="productOptions"
      placeholder="Select a product"
    />
    <AppSelect
      v-if="form.scope === 'Category'"
      v-model="form.categoryId"
      label="Category"
      clearable
      :options="categoryOptions"
      placeholder="Select a category"
    />

    <div class="row q-col-gutter-sm">
      <div class="col-6"><AppFieldLabel label="Starts" /><q-input v-model="form.startDate" dense outlined type="date" /></div>
      <div class="col-6"><AppFieldLabel label="Ends" /><q-input v-model="form.endDate" dense outlined type="date" /></div>
    </div>

    <AppTextField v-model="form.minimumOrderValue" label="Minimum order value" type="number" step="0.01" placeholder="Optional threshold" />

    <div class="column q-gutter-xs q-mt-sm">
      <q-toggle v-model="form.isExclusive" label="Exclusive (cannot combine with other discounts)" color="primary" />
      <q-toggle v-model="form.isActive" label="Active" color="primary" />
    </div>
  </AppFormDrawer>
</template>

<script setup>
/* DiscountFormDrawer (WO-115): create/edit a discount rule (scope, type, value, target, window, flags). */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { discountScopeOptions, discountTypeOptions } from 'modules/pricing/api'
import { productApi, categoryApi } from 'modules/catalog/api'
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

const EMPTY = {
  name: '', scope: 'CartTotal', type: 'Percentage', value: null,
  productId: null, categoryId: null, startDate: '', endDate: '', minimumOrderValue: null,
  isExclusive: false, isActive: true
}
const form = reactive({ ...EMPTY })

const rules = { name: { required, maxLength: maxLength(200) }, value: { required } }
const v$ = useVuelidate(rules, form)

const productOptions = ref([])
const categoryOptions = ref([])

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
function toNumberOrNull (v) { if (v === '' || v == null) return null; const n = Number(v); return Number.isFinite(n) ? n : null }

async function loadOptions () {
  try {
    const [p, c] = await Promise.all([
      productApi.list({ page: 1, pageSize: 200 }).catch(() => []),
      categoryApi.list({ page: 1, pageSize: 200 }).catch(() => [])
    ])
    const pItems = Array.isArray(p) ? p : p?.items || []
    const cItems = Array.isArray(c) ? c : c?.items || []
    productOptions.value = pItems.map((x) => ({ label: x.name, value: x.id }))
    categoryOptions.value = cItems.map((x) => ({ label: x.name, value: x.id }))
  } catch (e) { /* selects stay empty */ }
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    name: form.name.trim(),
    scope: form.scope,
    type: form.type,
    value: toNumberOrNull(form.value) || 0,
    productId: form.scope === 'Product' ? form.productId || null : null,
    categoryId: form.scope === 'Category' ? form.categoryId || null : null,
    startDateUtc: form.startDate || null,
    endDateUtc: form.endDate || null,
    minimumOrderValue: toNumberOrNull(form.minimumOrderValue),
    isExclusive: form.isExclusive,
    isActive: form.isActive
  })
}

onMounted(loadOptions)
</script>
