<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="isCreate ? 'New product' : (product?.name || 'Product')"
      :subtitle="!isCreate && product ? productTypeLabel(product.productType) : ''"
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Products', to: { name: 'catalog-products' } },
        { label: isCreate ? 'New product' : (product?.name || 'Product') }
      ]"
      :status="!isCreate && product ? (product.isPublished ? 'Published' : 'Draft') : ''"
      :status-color="product && product.isPublished ? 'positive' : 'grey'"
      show-back
      @back="router.back()"
    />

    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !isCreate && !product" class="bg-grey-2 rounded-borders">
      Product not found.
    </q-banner>

    <template v-if="isCreate || product">
      <!-- Basic information (create + edit) -->
      <AppSection title="Basic information">
        <ProductCoreForm ref="coreForm" :item="product" />
        <template #footer>
          <q-btn
            v-if="canWrite"
            unelevated
            color="primary"
            :label="isCreate ? 'Create product' : 'Save'"
            :loading="savingCore"
            no-caps
            @click="onSaveCore"
          />
        </template>
      </AppSection>

      <div v-if="isCreate" class="text-caption text-grey-7 q-px-sm q-pb-md">
        Save the basic information to create the product — categories, tags, prices, media and variants
        will then appear here to manage on this same page.
      </div>

      <!-- Sub-resource management (edit only) -->
      <template v-if="!isCreate && product">
        <!-- Categories -->
        <AppSection title="Categories">
          <q-select
            v-model="categoryIds"
            dense
            outlined
            multiple
            use-chips
            emit-value
            map-options
            :options="categoryOptions"
            :disable="!canWrite"
            label="Assigned categories"
          />
          <template #footer>
            <q-btn v-if="canWrite" unelevated color="primary" label="Save categories" no-caps :loading="savingCategories" @click="saveCategories" />
          </template>
        </AppSection>

        <!-- Tags -->
        <AppSection title="Tags">
          <q-select
            v-model="tagNames"
            dense
            outlined
            multiple
            use-input
            use-chips
            hide-dropdown-icon
            new-value-mode="add-unique"
            :disable="!canWrite"
            label="Tags"
            hint="Type a tag and press Enter"
          />
          <template #footer>
            <q-btn v-if="canWrite" unelevated color="primary" label="Save tags" no-caps :loading="savingTags" @click="saveTags" />
          </template>
        </AppSection>

        <!-- Tier prices -->
        <AppSection title="Tier prices">
          <template #actions>
            <q-btn v-if="canWrite" flat dense icon="o_add" label="Add tier" no-caps @click="addTier" />
          </template>
          <div v-if="!tiers.length" class="text-grey-6 q-py-sm">No tier prices.</div>
          <div v-for="(tier, i) in tiers" :key="i" class="row q-col-gutter-sm items-center q-mb-xs">
            <div class="col">
              <q-input v-model.number="tier.minQuantity" dense outlined type="number" label="Min quantity" :disable="!canWrite" />
            </div>
            <div class="col">
              <q-input v-model.number="tier.price" dense outlined type="number" step="0.01" label="Price" :disable="!canWrite" />
            </div>
            <div class="col-auto">
              <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="tiers.splice(i, 1)" />
            </div>
          </div>
          <template #footer>
            <q-btn v-if="canWrite" unelevated color="primary" label="Save tier prices" no-caps :loading="savingTiers" @click="saveTiers" />
          </template>
        </AppSection>

        <!-- Media -->
        <AppSection title="Media">
          <ProductGalleryView
            v-if="product.images && product.images.length"
            :images="normalizedImages"
            :product-name="product.name"
            class="q-mb-md"
          />
          <q-list v-if="product.images && product.images.length" bordered separator class="q-mb-md rounded-borders">
            <q-item v-for="img in product.images" :key="img.id">
              <q-item-section avatar>
                <q-badge outline :label="img.mediaType" />
              </q-item-section>
              <q-item-section class="ellipsis">{{ img.url }}</q-item-section>
              <q-item-section side>
                <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="removeImage(img)" />
              </q-item-section>
            </q-item>
          </q-list>
          <div v-else class="text-grey-6 q-mb-md">No media yet.</div>

          <template v-if="canWrite">
            <div class="text-caption text-grey-7 q-mb-xs">Add media</div>
            <div class="row q-col-gutter-sm">
              <div class="col-12 col-sm-6">
                <q-input v-model="newImage.url" dense outlined label="URL" />
              </div>
              <div class="col-6 col-sm-3">
                <q-select v-model="newImage.mediaType" dense outlined emit-value map-options :options="mediaTypeOptions" label="Type" />
              </div>
              <div class="col-6 col-sm-3">
                <q-input v-model.number="newImage.displayOrder" dense outlined type="number" label="Order" />
              </div>
              <div class="col-12 col-sm-6">
                <q-input v-model="newImage.thumbnailUrl" dense outlined label="Thumbnail URL" />
              </div>
              <div class="col-12 col-sm-6">
                <q-input v-model="newImage.altText" dense outlined label="Alt text" />
              </div>
            </div>
          </template>
          <template v-if="canWrite" #footer>
            <q-btn unelevated color="primary" label="Add media" no-caps :loading="addingImage" @click="addImage" />
          </template>
        </AppSection>

        <!-- Variants (WithVariants only) -->
        <AppSection v-if="product.productType === 'WithVariants'" title="Variants">
          <div class="text-caption text-grey-7 q-mb-xs">Attributes used for generation</div>
          <q-select
            v-model="attributeIds"
            dense
            outlined
            multiple
            use-chips
            emit-value
            map-options
            :options="attributeOptions"
            :disable="!canWrite"
            label="Attributes"
            class="q-mb-sm"
          />
          <div class="row q-gutter-sm q-mb-md">
            <q-btn v-if="canWrite" outline color="primary" label="Save attributes" no-caps :loading="savingAttributes" @click="saveAttributes" />
            <q-btn v-if="canWrite" unelevated color="primary" icon="o_auto_awesome" label="Generate variants" no-caps :loading="generating" @click="generate" />
          </div>

          <q-markup-table v-if="variants.length" flat bordered dense>
            <thead>
              <tr>
                <th class="text-left">Combination</th>
                <th class="text-left">SKU</th>
                <th class="text-right">Price</th>
                <th class="text-right">Stock</th>
                <th class="text-center">Enabled</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="variant in variants" :key="variant.id">
                <td class="text-left">{{ variantLabel(variant) }}</td>
                <td><q-input v-model="variant.sku" dense borderless :disable="!canWrite" /></td>
                <td><q-input v-model.number="variant.price" dense borderless type="number" step="0.01" input-class="text-right" :disable="!canWrite" /></td>
                <td><q-input v-model.number="variant.stockQuantity" dense borderless type="number" input-class="text-right" :disable="!canWrite" /></td>
                <td class="text-center"><q-toggle v-model="variant.isEnabled" color="primary" :disable="!canWrite" /></td>
                <td class="text-right">
                  <q-btn v-if="canWrite" flat round dense icon="o_save" color="primary" :loading="variant._saving" @click="saveVariant(variant)">
                    <q-tooltip>Save row</q-tooltip>
                  </q-btn>
                  <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="removeVariant(variant)" />
                </td>
              </tr>
            </tbody>
          </q-markup-table>
          <div v-else class="text-grey-6">No variants yet. Assign attributes (with values) then generate.</div>
        </AppSection>
      </template>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Product create + detail page (WO-15; unified). One full page serves both flows:
 * - Create mode (route `catalog-product-new`): only the Basic-information section is shown; on save the
 *   product is created and the page transitions to the manage view (all sub-resource sections appear).
 * - Edit mode (route `catalog-product-detail`): editable Basic-information + categories, tags, tier prices,
 *   media and (for WithVariants) variant generation/editing — each its own AppSection card, saved via its
 *   own replace-semantics endpoint.
 */
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getApiErrorMessage } from 'services/api'
import {
  productApi,
  categoryApi,
  productAttributeApi,
  mediaTypeOptions,
  productTypeLabel
} from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import ProductCoreForm from 'modules/catalog/components/ProductCoreForm.vue'
import ProductGalleryView from 'modules/catalog/components/ProductGalleryView.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const isCreate = computed(() => route.name === 'catalog-product-new')

const product = ref(null)
const loading = ref(false)

const coreForm = ref(null)
const savingCore = ref(false)

const categoryIds = ref([])
const categoryOptions = ref([])
const savingCategories = ref(false)

const tagNames = ref([])
const savingTags = ref(false)

const tiers = ref([])
const savingTiers = ref(false)

const newImage = reactive({ url: '', mediaType: 'Image', thumbnailUrl: '', altText: '', displayOrder: 0 })
const addingImage = ref(false)

const attributeIds = ref([])
const attributeOptions = ref([])
const attributeValueMap = ref({})
const savingAttributes = ref(false)
const generating = ref(false)
const variants = ref([])

// The gallery component keys media by id and reads productVariantId/mediaType.
const normalizedImages = computed(() => product.value?.images || [])

function hydrateFromProduct (p) {
  categoryIds.value = [...(p.categoryIds || [])]
  tagNames.value = (p.tags || []).map((t) => t.name)
  tiers.value = (p.tierPrices || [])
    .filter((t) => !t.productVariantId)
    .map((t) => ({ minQuantity: t.minQuantity, price: t.price }))
  attributeIds.value = [...(p.attributeIds || [])]
  variants.value = (p.variants || []).map((v) => ({
    id: v.id,
    sku: v.sku,
    price: v.price,
    stockQuantity: v.stockQuantity,
    allowBackorder: v.allowBackorder,
    isEnabled: v.isEnabled,
    displayOrder: v.displayOrder,
    attributeValueIds: v.attributeValueIds || [],
    _saving: false
  }))
}

async function load () {
  loading.value = true
  try {
    product.value = await productApi.get(route.params.id)
    hydrateFromProduct(product.value)
  } catch (err) {
    product.value = null
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

// Re-initialise on mount and whenever the route changes (create → detail after save, or id → id).
async function init () {
  if (isCreate.value) {
    product.value = null
    loading.value = false
    return
  }
  await load()
}

async function onSaveCore () {
  const payload = await coreForm.value?.submit()
  if (!payload) return

  savingCore.value = true
  try {
    if (isCreate.value) {
      const created = await productApi.create(payload)
      notify.success('Product created')
      // Same component: switch to the manage route; the route watcher reloads the full graph.
      router.replace({ name: 'catalog-product-detail', params: { id: created.id } })
    } else {
      await productApi.update(route.params.id, payload)
      notify.success('Product updated')
      await load()
    }
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    savingCore.value = false
  }
}

function flattenTree (nodes, depth = 0, acc = []) {
  for (const node of nodes || []) {
    acc.push({ label: `${'— '.repeat(depth)}${node.name}`, value: node.id })
    if (node.children && node.children.length) flattenTree(node.children, depth + 1, acc)
  }
  return acc
}

async function loadCategories () {
  try {
    const tree = await categoryApi.tree()
    categoryOptions.value = flattenTree(Array.isArray(tree) ? tree : [])
  } catch (err) {
    categoryOptions.value = []
  }
}

async function loadAttributes () {
  try {
    const result = await productAttributeApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    attributeOptions.value = items.map((a) => ({ label: a.name, value: a.id }))
    const map = {}
    for (const attr of items) {
      for (const val of attr.values || []) {
        map[val.id] = `${attr.name}: ${val.value}`
      }
    }
    attributeValueMap.value = map
  } catch (err) {
    attributeOptions.value = []
  }
}

function variantLabel (variant) {
  const parts = (variant.attributeValueIds || []).map((id) => attributeValueMap.value[id] || id)
  return parts.length ? parts.join(', ') : '(no attributes)'
}

async function saveCategories () {
  savingCategories.value = true
  try {
    product.value = await productApi.setCategories(route.params.id, categoryIds.value)
    hydrateFromProduct(product.value)
    notify.success('Categories saved')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    savingCategories.value = false
  }
}

async function saveTags () {
  savingTags.value = true
  try {
    product.value = await productApi.setTags(route.params.id, tagNames.value)
    hydrateFromProduct(product.value)
    notify.success('Tags saved')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    savingTags.value = false
  }
}

function addTier () {
  tiers.value.push({ minQuantity: 1, price: 0 })
}

async function saveTiers () {
  savingTiers.value = true
  try {
    const payload = tiers.value.map((t) => ({
      minQuantity: Number(t.minQuantity) || 0,
      price: Number(t.price) || 0
    }))
    product.value = await productApi.setTierPrices(route.params.id, payload)
    hydrateFromProduct(product.value)
    notify.success('Tier prices saved')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    savingTiers.value = false
  }
}

async function addImage () {
  if (!newImage.url) {
    notify.warning('Enter a media URL')
    return
  }
  addingImage.value = true
  try {
    await productApi.addImage(route.params.id, {
      productVariantId: null,
      mediaType: newImage.mediaType,
      url: newImage.url,
      thumbnailUrl: newImage.thumbnailUrl || null,
      altText: newImage.altText || null,
      displayOrder: Number(newImage.displayOrder) || 0
    })
    notify.success('Media added')
    Object.assign(newImage, { url: '', mediaType: 'Image', thumbnailUrl: '', altText: '', displayOrder: 0 })
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    addingImage.value = false
  }
}

async function removeImage (img) {
  if (!(await deleteConfirmation('this media entry'))) return
  try {
    await productApi.deleteImage(img.id)
    notify.success('Media deleted')
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

async function saveAttributes () {
  savingAttributes.value = true
  try {
    product.value = await productApi.setAttributes(route.params.id, attributeIds.value)
    hydrateFromProduct(product.value)
    notify.success('Attributes saved')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    savingAttributes.value = false
  }
}

async function generate () {
  generating.value = true
  try {
    product.value = await productApi.generateVariants(route.params.id)
    hydrateFromProduct(product.value)
    notify.success('Variants generated')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    generating.value = false
  }
}

async function saveVariant (variant) {
  variant._saving = true
  try {
    await productApi.updateVariant(variant.id, {
      sku: variant.sku || null,
      price: variant.price === '' || variant.price === null ? null : Number(variant.price),
      stockQuantity: Number(variant.stockQuantity) || 0,
      allowBackorder: variant.allowBackorder,
      isEnabled: variant.isEnabled,
      displayOrder: Number(variant.displayOrder) || 0
    })
    notify.success('Variant saved')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    variant._saving = false
  }
}

async function removeVariant (variant) {
  if (!(await deleteConfirmation('this variant'))) return
  try {
    await productApi.deleteVariant(variant.id)
    notify.success('Variant deleted')
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => {
  init()
  loadCategories()
  loadAttributes()
})

// After create we router.replace to the detail route on the same component instance — reload on change.
watch(() => route.fullPath, () => init())
</script>
