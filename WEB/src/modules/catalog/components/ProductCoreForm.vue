<template>
  <div>
    <div class="row q-col-gutter-sm">
      <div class="col-12 col-md-4">
        <AppSelect v-model="form.productType" label="Type" required :options="productTypeOptions" :v="v$.productType" />
      </div>
      <div class="col-12 col-md-8">
        <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Men's Red Polo Shirt" />
      </div>
    </div>

    <div class="row q-col-gutter-sm">
      <div class="col-12 col-md-6">
        <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" placeholder="Auto-generated from the name if left blank" />
      </div>
      <div class="col-12 col-md-6">
        <AppTextField v-model="form.sku" label="SKU" :v="v$.sku" placeholder="e.g. TSHIRT-RED-M" />
      </div>
    </div>

    <AppTextField
      v-model="form.shortDescription"
      label="Short description"
      type="textarea"
      autogrow
      placeholder="A brief one- or two-line summary shown on product cards and search results"
    />
    <AppRichText
      v-model="form.fullDescription"
      label="Full description"
      placeholder="Describe the product in detail — features, materials, sizing…"
    />

    <div class="row q-col-gutter-sm">
      <div class="col-6 col-md-3">
        <AppTextField v-model="form.price" label="Price" type="number" step="0.01" placeholder="0.00" />
      </div>
      <div class="col-6 col-md-3">
        <AppTextField v-model="form.stockQuantity" label="Stock quantity" type="number" placeholder="0" />
      </div>
      <div class="col-12 col-md-6">
        <AppSelect v-model="form.taxCategoryId" label="Tax category" required :options="taxCategoryOptions" :v="v$.taxCategoryId" />
      </div>
    </div>
    <div v-if="!taxCategoryOptions.length" class="text-caption text-negative q-mb-sm">
      No tax categories exist yet. Create one first (a default “Standard” category is seeded automatically).
    </div>

    <div class="row q-col-gutter-sm items-center">
      <div class="col-12 col-md-4">
        <AppSelect v-model="form.manufacturerId" label="Manufacturer" :options="manufacturerOptions" clearable />
      </div>
      <div class="col-6 col-md-2">
        <AppTextField v-model="form.displayOrder" label="Display order" type="number" />
      </div>
      <div class="col-auto">
        <q-toggle v-model="form.allowBackorder" label="Allow backorder" color="primary" />
      </div>
      <div class="col-auto">
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
      <div class="row q-col-gutter-sm">
        <div class="col-12 col-md-6">
          <AppSelect v-model="form.giftCardType" label="Gift card type" :options="giftCardTypeOptions" />
        </div>
        <div class="col-12 col-md-6">
          <AppTextField v-model="form.giftCardAmount" label="Gift card amount" type="number" step="0.01" />
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
/*
 * ProductCoreForm (WO-15; extracted from the former ProductFormDrawer): the Vuelidate-validated
 * scalar-configuration fields for a product, laid out for a full-page "Basic information" section.
 * Exposes an async `submit()` that validates and returns the create/update payload (or null when
 * invalid) so the host page owns the Save button + persistence.
 */
import { reactive, watch, onMounted, ref } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { manufacturerApi, taxCategoryApi, productTypeOptions, giftCardTypeOptions } from 'modules/catalog/api'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'

const props = defineProps({
  item: { type: Object, default: null }
})

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
const taxCategoryOptions = ref([])

async function loadManufacturers () {
  try {
    const result = await manufacturerApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    manufacturerOptions.value = items.map((m) => ({ label: m.name, value: m.id }))
  } catch (err) {
    manufacturerOptions.value = []
  }
}

async function loadTaxCategories () {
  try {
    const result = await taxCategoryApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    taxCategoryOptions.value = items.map((t) => ({ label: t.name, value: t.id }))
    applyDefaultTaxCategory()
  } catch (err) {
    taxCategoryOptions.value = []
  }
}

// For a new product, preselect the first tax category so the required field is satisfied out of the box.
function applyDefaultTaxCategory () {
  if (!form.taxCategoryId && taxCategoryOptions.value.length) {
    form.taxCategoryId = taxCategoryOptions.value[0].value
  }
}

onMounted(() => {
  loadManufacturers()
  loadTaxCategories()
})

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item || {})
    applyDefaultTaxCategory()
    v$.value.$reset()
  },
  { immediate: true }
)

function toNumberOrNull (value) {
  if (value === '' || value === null || value === undefined) return null
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

function buildPayload () {
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

  return payload
}

// Host page calls this on Save: validates and returns the payload, or null when invalid.
async function submit () {
  const ok = await v$.value.$validate()
  return ok ? buildPayload() : null
}

defineExpose({ submit })
</script>
