<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit product' : 'New product'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppSelect
      v-model="form.productType"
      label="Type"
      required
      :options="productTypeOptions"
      :v="v$.productType"
    />
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" />
    <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" />
    <AppTextField v-model="form.sku" label="SKU" :v="v$.sku" />
    <AppTextField
      v-model="form.shortDescription"
      label="Short description"
      type="textarea"
      autogrow
    />
    <AppTextField
      v-model="form.fullDescription"
      label="Full description"
      type="textarea"
      autogrow
    />

    <div class="row q-col-gutter-sm">
      <div class="col-6">
        <AppTextField v-model="form.price" label="Price" type="number" step="0.01" />
      </div>
      <div class="col-6">
        <AppTextField v-model="form.stockQuantity" label="Stock quantity" type="number" />
      </div>
    </div>

    <q-toggle v-model="form.allowBackorder" label="Allow backorder" color="primary" class="q-mb-sm" />

    <AppTextField v-model="form.taxCategoryId" label="Tax category id" required :v="v$.taxCategoryId" />
    <div class="text-caption text-grey-6 q-mb-sm">
      No tax-category list endpoint exists yet — paste a Tax Category UUID (required).
    </div>

    <AppSelect
      v-model="form.manufacturerId"
      label="Manufacturer"
      :options="manufacturerOptions"
      clearable
    />

    <div class="row q-col-gutter-sm items-center">
      <div class="col-6">
        <AppTextField v-model="form.displayOrder" label="Display order" type="number" />
      </div>
      <div class="col-6">
        <q-toggle v-model="form.isPublished" label="Published" color="primary" />
      </div>
    </div>

    <!-- Downloadable-only fields -->
    <template v-if="form.productType === 'Downloadable'">
      <q-separator class="q-my-sm" />
      <div class="text-caption text-grey-7 q-mb-xs">Download settings</div>
      <div class="row q-col-gutter-sm">
        <div class="col-6">
          <AppTextField v-model="form.downloadExpiryDays" label="Download expiry (days)" type="number" />
        </div>
        <div class="col-6">
          <AppTextField v-model="form.downloadLimit" label="Download limit" type="number" />
        </div>
      </div>
    </template>

    <!-- Gift-card-only fields -->
    <template v-if="form.productType === 'GiftCard'">
      <q-separator class="q-my-sm" />
      <div class="text-caption text-grey-7 q-mb-xs">Gift card settings</div>
      <AppSelect v-model="form.giftCardType" label="Gift card type" :options="giftCardTypeOptions" />
      <AppTextField v-model="form.giftCardAmount" label="Gift card amount" type="number" step="0.01" />
    </template>
  </AppFormDrawer>
</template>

<script setup>
/*
 * ProductFormDrawer (WO-15): Vuelidate-validated create/edit form for a
 * product's scalar configuration. Type-specific fields (downloadable / gift
 * card) render conditionally. Sub-resources (categories, tags, tier prices,
 * media, variants) are managed on the product detail page after the product
 * has been created.
 */
import { reactive, computed, watch, onMounted, ref } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { manufacturerApi, productTypeOptions, giftCardTypeOptions } from 'modules/catalog/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = {
  productType: 'Simple',
  name: '',
  slug: '',
  sku: '',
  shortDescription: '',
  fullDescription: '',
  price: null,
  stockQuantity: 0,
  allowBackorder: false,
  taxCategoryId: '',
  manufacturerId: null,
  isPublished: false,
  displayOrder: 0,
  downloadExpiryDays: null,
  downloadLimit: null,
  giftCardType: 'Fixed',
  giftCardAmount: null
}
const form = reactive({ ...EMPTY })

const rules = {
  name: { required, maxLength: maxLength(400) },
  productType: { required },
  taxCategoryId: { required },
  slug: { maxLength: maxLength(400) },
  sku: { maxLength: maxLength(400) }
}
const v$ = useVuelidate(rules, form)

const manufacturerOptions = ref([])

async function loadManufacturers () {
  try {
    const result = await manufacturerApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    manufacturerOptions.value = items.map((m) => ({ label: m.name, value: m.id }))
  } catch (err) {
    manufacturerOptions.value = []
  }
}

onMounted(loadManufacturers)

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item || {})
    v$.value.$reset()
  },
  { immediate: true }
)

function toNumberOrNull (value) {
  if (value === '' || value === null || value === undefined) return null
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return

  const payload = {
    name: form.name,
    productType: form.productType,
    taxCategoryId: form.taxCategoryId,
    slug: form.slug || null,
    shortDescription: form.shortDescription || null,
    fullDescription: form.fullDescription || null,
    sku: form.sku || null,
    price: toNumberOrNull(form.price),
    stockQuantity: toNumberOrNull(form.stockQuantity) || 0,
    allowBackorder: form.allowBackorder,
    manufacturerId: form.manufacturerId || null,
    isPublished: form.isPublished,
    displayOrder: toNumberOrNull(form.displayOrder) || 0
  }

  if (form.productType === 'Downloadable') {
    payload.downloadExpiryDays = toNumberOrNull(form.downloadExpiryDays)
    payload.downloadLimit = toNumberOrNull(form.downloadLimit)
  }

  if (form.productType === 'GiftCard') {
    payload.giftCardType = form.giftCardType
    payload.giftCardAmount = toNumberOrNull(form.giftCardAmount)
  }

  emit('submit', payload)
}
</script>
