<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="isCreate ? 'New product' : (product?.name || 'Product')"
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Products', to: { name: 'catalog-products' } },
        { label: isCreate ? 'New product' : (product?.name || 'Product') }
      ]"
      :status="!isCreate && product ? (form.isPublished ? 'Published' : 'Draft') : ''"
      :status-color="form.isPublished ? 'positive' : 'grey'"
      show-back
      @back="router.push({ name: 'catalog-products' })"
    >
      <template #actions>
        <q-chip
          v-if="saveStatus"
          :icon="saveStatus.icon"
          :color="saveStatus.chip"
          :text-color="saveStatus.text"
          square
          dense
          class="q-mr-sm text-caption"
        >
          <q-spinner v-if="saveStatus.spin" size="14px" class="q-mr-xs" />
          {{ saveStatus.label }}
        </q-chip>
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !isCreate && !product" class="bg-grey-2 rounded-borders">
      Product not found.
    </q-banner>

    <template v-if="isCreate || product">
      <!-- Auto-save hint (edit only) -->
      <div v-if="!isCreate" class="row items-center text-caption text-grey-7 q-mb-sm q-px-xs">
        <q-icon name="o_cloud_sync" size="16px" class="q-mr-xs" />
        Changes are saved automatically as you edit — no need to press save.
      </div>

      <q-card flat bordered class="app-section">
        <q-tabs
          v-model="tab"
          align="left"
          active-color="primary"
          indicator-color="primary"
          class="text-grey-7 app-product-tabs"
          no-caps
          inline-label
        >
          <q-tab name="general" icon="o_info" label="General" />
          <q-tab name="pricing" icon="o_sell" label="Pricing &amp; inventory" :disable="isCreate" />
          <q-tab name="organization" :disable="isCreate">
            <div class="row items-center no-wrap">
              <q-icon name="o_account_tree" class="q-mr-xs" />Organization
              <q-badge v-if="!isCreate && orgCount" color="grey-5" class="q-ml-xs">{{ orgCount }}</q-badge>
            </div>
          </q-tab>
          <q-tab name="media" :disable="isCreate">
            <div class="row items-center no-wrap">
              <q-icon name="o_photo_library" class="q-mr-xs" />Media
              <q-badge v-if="!isCreate && mediaCount" color="grey-5" class="q-ml-xs">{{ mediaCount }}</q-badge>
            </div>
          </q-tab>
          <q-tab name="seo" icon="o_search" label="SEO" :disable="isCreate" />
          <q-tab v-if="isVariantType" name="variants" :disable="isCreate">
            <div class="row items-center no-wrap">
              <q-icon name="o_style" class="q-mr-xs" />Variants
              <q-badge v-if="!isCreate && variants.length" color="grey-5" class="q-ml-xs">{{ variants.length }}</q-badge>
            </div>
          </q-tab>
        </q-tabs>

        <q-separator />

        <q-tab-panels v-model="tab" animated keep-alive>
          <!-- ============ GENERAL ============ -->
          <q-tab-panel name="general" class="q-gutter-y-xs">
            <div class="row q-col-gutter-sm">
              <div class="col-12 col-md-4">
                <AppSelect v-model="form.productType" label="Type" required :options="productTypeOptions" :v="v$.productType" :disable="!canWrite" hint="Determines which extra fields apply" />
              </div>
              <div class="col-12 col-md-8">
                <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Men's Red Polo Shirt" :disable="!canWrite" />
              </div>
            </div>

            <div class="row q-col-gutter-sm">
              <div class="col-12 col-md-6">
                <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" placeholder="e.g. mens-red-polo-shirt" hint="Storefront URL — auto-filled from the name until you edit it" :disable="!canWrite" />
              </div>
              <div class="col-12 col-md-6">
                <AppTextField v-model="form.sku" label="SKU" :v="v$.sku" placeholder="e.g. MENS-RED-POLO-SHIRT" hint="Stock code — auto-filled from the name until you edit it" :disable="!canWrite" />
              </div>
            </div>

            <AppTextField
              v-model="form.shortDescription"
              label="Short description"
              type="textarea"
              autogrow
              placeholder="A brief one- or two-line summary shown on product cards and search results"
              :disable="!canWrite"
            />
            <AppRichText
              v-model="form.fullDescription"
              label="Full description"
              placeholder="Describe the product in detail — features, materials, sizing…"
              hint="Rich text shown on the product page"
              :disable="!canWrite"
            />

            <q-separator class="q-my-sm" />
            <div class="row q-col-gutter-sm items-start">
              <div class="col-12 col-md-6">
                <AppSelect v-model="form.taxCategoryId" label="Tax category" required :options="taxCategoryOptions" :v="v$.taxCategoryId" :disable="!canWrite" hint="Drives tax calculation at checkout" />
                <div v-if="!taxCategoryOptions.length" class="text-caption text-negative">
                  No tax categories exist yet. A default “Standard” category is normally seeded automatically.
                </div>
              </div>
              <div class="col-12 col-md-6">
                <AppSelect v-model="form.manufacturerId" label="Manufacturer" :options="manufacturerOptions" clearable placeholder="None" :disable="!canWrite" />
              </div>
            </div>

            <div class="row q-col-gutter-sm items-center">
              <div class="col-6 col-md-3">
                <AppTextField v-model="form.displayOrder" label="Display order" type="number" hint="Lower shows first" :disable="!canWrite" />
              </div>
              <div class="col-auto q-mt-md">
                <q-toggle v-model="form.isPublished" label="Published" color="primary" :disable="!canWrite" />
              </div>
              <div class="col-auto q-mt-md">
                <q-toggle v-model="form.isFeatured" label="Featured" color="primary" :disable="!canWrite" />
              </div>
            </div>

            <div v-if="form.isFeatured" class="row q-col-gutter-sm items-center">
              <div class="col-6 col-md-3">
                <AppTextField v-model="form.featuredDisplayOrder" label="Featured order" type="number" hint="Order among featured products" :disable="!canWrite" />
              </div>
            </div>
          </q-tab-panel>

          <!-- ============ PRICING & INVENTORY ============ -->
          <q-tab-panel name="pricing" class="q-gutter-y-sm">
            <div class="row q-col-gutter-sm">
              <div class="col-6 col-md-4">
                <AppTextField v-model="form.price" label="Price" type="number" step="0.01" placeholder="0.00" hint="Base price in the store currency" :disable="!canWrite" />
              </div>
              <div class="col-6 col-md-4">
                <AppTextField v-model="form.stockQuantity" label="Stock quantity" type="number" placeholder="0" :disable="!canWrite" />
              </div>
            </div>

            <div class="row items-center q-gutter-md q-mt-none">
              <q-toggle v-model="form.allowBackorder" label="Allow backorder" color="primary" :disable="!canWrite" />
              <div v-if="form.allowBackorder" style="max-width: 220px">
                <AppFieldLabel label="Estimated restock date">
                  <template #hint>Shown to buyers on backordered items</template>
                </AppFieldLabel>
                <q-input v-model="form.estimatedRestockDate" dense outlined type="date" :disable="!canWrite" clearable />
              </div>
            </div>

            <!-- Downloadable settings -->
            <template v-if="form.productType === 'Downloadable'">
              <q-separator class="q-my-sm" />
              <AppFieldLabel label="Download settings" />
              <div class="row q-col-gutter-sm">
                <div class="col-6"><AppTextField v-model="form.downloadExpiryDays" label="Download expiry (days)" type="number" placeholder="e.g. 30" :disable="!canWrite" /></div>
                <div class="col-6"><AppTextField v-model="form.downloadLimit" label="Download limit" type="number" placeholder="e.g. 5" :disable="!canWrite" /></div>
              </div>
            </template>

            <!-- Gift card settings -->
            <template v-if="form.productType === 'GiftCard'">
              <q-separator class="q-my-sm" />
              <AppFieldLabel label="Gift card settings" />
              <div class="row q-col-gutter-sm">
                <div class="col-12 col-md-6"><AppSelect v-model="form.giftCardType" label="Gift card type" :options="giftCardTypeOptions" :disable="!canWrite" /></div>
                <div class="col-12 col-md-6"><AppTextField v-model="form.giftCardAmount" label="Gift card amount" type="number" step="0.01" placeholder="0.00" :disable="!canWrite" /></div>
              </div>
            </template>

            <q-separator class="q-my-sm" />
            <div class="row items-center justify-between q-mb-xs">
              <AppFieldLabel label="Tier prices">
                <template #hint>Quantity-break pricing — a lower price once the buyer's quantity reaches the threshold</template>
              </AppFieldLabel>
              <q-btn v-if="canWrite" flat dense no-caps color="primary" icon="o_add" label="Add tier" @click="addTier" />
            </div>
            <div v-if="!tiers.length" class="text-grey-6 text-caption q-mb-sm">No tier prices — every quantity uses the base price.</div>
            <div v-for="(tier, i) in tiers" :key="i" class="row q-col-gutter-sm items-center q-mb-xs no-wrap">
              <div class="col"><q-input v-model.number="tier.minQuantity" dense outlined type="number" label="Min quantity" :disable="!canWrite" @update:model-value="queueTiers" /></div>
              <div class="col"><q-input v-model.number="tier.price" dense outlined type="number" step="0.01" label="Price" :disable="!canWrite" @update:model-value="queueTiers" /></div>
              <div class="col-auto"><q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="removeTier(i)" /></div>
            </div>
          </q-tab-panel>

          <!-- ============ ORGANIZATION ============ -->
          <q-tab-panel name="organization" class="q-gutter-y-sm">
            <AppFieldLabel label="Categories">
              <template #hint>Where this product appears in the storefront category tree</template>
            </AppFieldLabel>
            <q-select
              v-model="categoryIds"
              dense outlined multiple use-chips emit-value map-options
              :options="categoryOptions"
              :disable="!canWrite"
              placeholder="Assign one or more categories"
              @update:model-value="queueCategories"
            />

            <q-separator class="q-my-md" />
            <AppFieldLabel label="Tags">
              <template #hint>Free-form keywords for search and merchandising — type and press Enter</template>
            </AppFieldLabel>
            <q-select
              v-model="tagNames"
              dense outlined multiple use-input use-chips hide-dropdown-icon
              new-value-mode="add-unique"
              :disable="!canWrite"
              placeholder="Add tags"
              @update:model-value="queueTags"
            />
          </q-tab-panel>

          <!-- ============ MEDIA ============ -->
          <q-tab-panel name="media" class="q-gutter-y-sm">
            <!-- Images: centralized media library (two-step upload → ProductPicture) -->
            <AppFieldLabel label="Images">
              <template #hint>Uploaded to the media library; display order follows the upload sequence</template>
            </AppFieldLabel>
            <div v-if="imagePictures.length" class="row q-col-gutter-sm q-mb-sm">
              <div v-for="pic in imagePictures" :key="pic.id" class="col-auto">
                <div class="product-pic">
                  <img :src="$media(pic.url)" :alt="pic.altText || (product && product.name)" class="product-pic__img" @click="openPicture(pic)">
                  <q-btn v-if="canWrite" round dense size="xs" color="negative" icon="o_close" class="product-pic__remove" @click="removePicture(pic)" />
                </div>
              </div>
            </div>
            <div v-else class="text-grey-6 text-caption q-mb-sm">No images yet.</div>

            <q-file
              v-if="canWrite"
              v-model="imageFiles"
              multiple dense outlined
              accept="image/*"
              label="Upload images"
              hint="PNG, JPG, GIF, WEBP — added to the media library"
              :loading="uploadingImages"
              @update:model-value="onImageFiles"
            >
              <template #prepend><q-icon name="o_add_photo_alternate" /></template>
            </q-file>

            <!-- Videos: embed URLs (legacy gallery — embeds aren't file uploads) -->
            <q-separator class="q-my-md" />
            <AppFieldLabel label="Videos">
              <template #hint>Paste an embed URL (YouTube, Vimeo…) and press Enter</template>
            </AppFieldLabel>
            <q-list v-if="videoImages.length" bordered separator class="q-mb-sm rounded-borders">
              <q-item v-for="img in videoImages" :key="img.id">
                <q-item-section avatar><q-icon name="o_movie" color="grey-7" /></q-item-section>
                <q-item-section class="ellipsis">{{ img.url }}</q-item-section>
                <q-item-section side>
                  <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="removeImage(img)" />
                </q-item-section>
              </q-item>
            </q-list>
            <q-input
              v-if="canWrite"
              v-model="newVideoUrl"
              dense outlined
              placeholder="e.g. https://www.youtube.com/embed/…"
              :loading="addingVideo"
              @keyup.enter="addVideo"
            >
              <template #prepend><q-icon name="o_movie" /></template>
              <template #append>
                <q-btn flat dense round icon="o_add" :disable="!newVideoUrl" @click="addVideo"><q-tooltip>Add video</q-tooltip></q-btn>
              </template>
            </q-input>

            <MediaSeoDialog v-model="lightboxOpen" :media-id="selectedMediaId" :fallback-url="lightboxUrl" @saved="onPictureSaved" />
          </q-tab-panel>

          <!-- ============ SEO ============ -->
          <q-tab-panel name="seo" class="q-gutter-y-sm">
            <!-- Search-result preview -->
            <div class="text-caption text-grey-7 q-mb-xs">Search engine preview</div>
            <q-card flat bordered class="q-pa-md q-mb-md seo-preview">
              <div class="seo-preview__title ellipsis">{{ seoPreview.title }}</div>
              <div class="seo-preview__url ellipsis">{{ seoPreview.url }}</div>
              <div class="seo-preview__desc">{{ seoPreview.description }}</div>
            </q-card>

            <AppTextField v-model="form.metaTitle" label="Meta title" placeholder="Defaults to the product name" :disable="!canWrite" maxlength="300">
              <template #hint>{{ metaTitleHint }}</template>
            </AppTextField>

            <AppTextField v-model="form.metaDescription" label="Meta description" type="textarea" autogrow placeholder="Defaults to the short description" :disable="!canWrite" maxlength="500">
              <template #hint>{{ metaDescriptionHint }}</template>
            </AppTextField>

            <AppTextField v-model="form.metaKeywords" label="Meta keywords" placeholder="e.g. polo shirt, cotton, menswear" :disable="!canWrite" maxlength="500">
              <template #hint>Comma-separated keywords (minor SEO impact on modern search engines)</template>
            </AppTextField>
          </q-tab-panel>

          <!-- ============ VARIANTS ============ -->
          <q-tab-panel v-if="isVariantType" name="variants" class="q-gutter-y-sm">
            <AppFieldLabel label="Attributes used for generation">
              <template #hint>Pick the attributes (with values) whose combinations become variants</template>
            </AppFieldLabel>
            <div class="row items-start q-col-gutter-sm">
              <div class="col">
                <q-select
                  v-model="attributeIds"
                  dense outlined multiple use-chips emit-value map-options
                  :options="attributeOptions"
                  :disable="!canWrite"
                  placeholder="Select attributes"
                  @update:model-value="queueAttributes"
                />
              </div>
              <div class="col-auto">
                <q-btn v-if="canWrite" unelevated color="primary" icon="o_auto_awesome" label="Generate variants" no-caps :loading="generating" @click="generate" />
              </div>
            </div>

            <q-markup-table v-if="variants.length" flat bordered dense class="q-mt-sm">
              <thead>
                <tr>
                  <th class="text-left">Combination</th>
                  <th class="text-left">SKU</th>
                  <th class="text-right">Price</th>
                  <th class="text-right">Stock</th>
                  <th class="text-center">Enabled</th>
                  <th class="text-center" style="width: 84px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="variant in variants" :key="variant.id">
                  <td class="text-left">{{ variantLabel(variant) }}</td>
                  <td><q-input v-model="variant.sku" dense borderless :disable="!canWrite" @update:model-value="queueVariant(variant)" /></td>
                  <td><q-input v-model.number="variant.price" dense borderless type="number" step="0.01" input-class="text-right" :disable="!canWrite" @update:model-value="queueVariant(variant)" /></td>
                  <td><q-input v-model.number="variant.stockQuantity" dense borderless type="number" input-class="text-right" :disable="!canWrite" @update:model-value="queueVariant(variant)" /></td>
                  <td class="text-center"><q-toggle v-model="variant.isEnabled" color="primary" :disable="!canWrite" @update:model-value="queueVariant(variant)" /></td>
                  <td class="text-center">
                    <q-spinner v-if="variant._saving" size="16px" color="primary" />
                    <q-icon v-else-if="variant._saved" name="o_check_circle" color="positive" size="18px"><q-tooltip>Saved</q-tooltip></q-icon>
                    <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="removeVariant(variant)" />
                  </td>
                </tr>
              </tbody>
            </q-markup-table>
            <div v-else class="text-grey-6 q-mt-sm">No variants yet. Assign attributes (with values) then Generate.</div>
          </q-tab-panel>
        </q-tab-panels>

        <!-- Create action: the only save button — you can't auto-save a product that doesn't exist yet -->
        <template v-if="isCreate">
          <q-separator />
          <q-card-actions class="q-pa-md">
            <div class="text-caption text-grey-7">
              Create the product to unlock pricing, organization, media{{ isVariantType ? ' and variants' : '' }} — all auto-saved from then on.
            </div>
            <q-space />
            <q-btn v-if="canWrite" unelevated color="primary" no-caps icon="o_check" label="Create product" :loading="creating" @click="createProduct" />
          </q-card-actions>
        </template>
      </q-card>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Product create + manage page (WO-15/WO-123; auto-save redesign). One page, two modes:
 * - Create: only the General tab is enabled; the other tabs are locked until the product exists.
 *   A single "Create product" action persists it, then redirects to the manage view.
 * - Manage (edit): tabbed sections (General / Pricing & inventory / Organization / Media / Variants)
 *   with NO save buttons — every field auto-saves on change (debounced), surfaced by the live status
 *   chip in the header ("Saving…" / "All changes saved"). Add / generate / delete stay as explicit
 *   operations; the core product and each sub-resource persist via their own replace-semantics endpoints.
 */
import { ref, reactive, computed, onMounted, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { debounce } from 'quasar'
import { getApiErrorMessage } from 'services/api'
import {
  productApi, mediaApi, categoryApi, productAttributeApi, manufacturerApi, taxCategoryApi,
  productTypeOptions, giftCardTypeOptions
} from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppRichText from 'components/common/AppRichText.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const isCreate = computed(() => route.name === 'catalog-product-new')
const pid = computed(() => route.params.id)

const product = ref(null)
const loading = ref(false)
const tab = ref('general')

// ---- Core scalar form ---------------------------------------------------------
const EMPTY = {
  productType: 'Simple', name: '', slug: '', sku: '', shortDescription: '', fullDescription: '',
  metaTitle: '', metaDescription: '', metaKeywords: '',
  price: null, stockQuantity: 0, allowBackorder: false, estimatedRestockDate: null,
  taxCategoryId: '', manufacturerId: null,
  isPublished: false, displayOrder: 0, isFeatured: false, featuredDisplayOrder: 0,
  downloadExpiryDays: null, downloadLimit: null,
  giftCardType: 'Fixed', giftCardAmount: null
}
const CORE_KEYS = Object.keys(EMPTY)
const form = reactive({ ...EMPTY })
const rules = {
  name: { required, maxLength: maxLength(400) },
  productType: { required },
  taxCategoryId: { required },
  slug: { maxLength: maxLength(400) },
  sku: { maxLength: maxLength(400) }
}
const v$ = useVuelidate(rules, form)

const isVariantType = computed(() => form.productType === 'WithVariants')

// ---- Options ------------------------------------------------------------------
const manufacturerOptions = ref([])
const taxCategoryOptions = ref([])
const categoryOptions = ref([])
const attributeOptions = ref([])
const attributeValueMap = ref({})

// ---- Sub-resources ------------------------------------------------------------
const categoryIds = ref([])
const tagNames = ref([])
const tiers = ref([])
const attributeIds = ref([])
const variants = ref([])
const pictures = ref([])            // Media-backed product images [{ id, mediaId, url, altText, displayOrder }]
const imageFiles = ref(null)        // q-file selection, processed then cleared
const uploadingImages = ref(false)
const lightboxOpen = ref(false)
const lightboxUrl = ref('')
const selectedMediaId = ref(null)
const newVideoUrl = ref('')
const addingVideo = ref(false)
const generating = ref(false)

// Pictures now hold both images and video embeds (unified Media-backed gallery). Split by media type.
const imagePictures = computed(() => pictures.value.filter((p) => p.mediaType === 'Image'))
const videoImages = computed(() => pictures.value.filter((p) => p.mediaType === 'Video'))
const mediaCount = computed(() => pictures.value.length)
const orgCount = computed(() => categoryIds.value.length + tagNames.value.length)

// SEO: a Google-style search-result preview + character-budget hints. Meta fields fall back to the
// product name / short description, mirroring how the storefront renders the page's <head>.
const seoPreview = computed(() => {
  const raw = form.metaDescription || form.shortDescription || 'A description of this product will appear here in search results.'
  return {
    title: form.metaTitle || form.name || 'Product title',
    url: `yourstore.com › shop › ${form.slug || 'product-slug'}`,
    description: raw.length > 160 ? `${raw.slice(0, 157)}…` : raw
  }
})
function lengthHint (value, ideal, fallbackLabel) {
  const n = (value || '').length
  if (!n) return `Falls back to the ${fallbackLabel}. Aim for ~${ideal} characters.`
  const note = n > ideal ? ' — may be truncated in results' : (n < ideal * 0.5 ? ' — consider adding detail' : ' — good length')
  return `${n} / ${ideal} recommended${note}`
}
const metaTitleHint = computed(() => lengthHint(form.metaTitle, 60, 'product name'))
const metaDescriptionHint = computed(() => lengthHint(form.metaDescription, 160, 'short description'))

// ---- Auto-save status ---------------------------------------------------------
const saving = ref(0)
const saveError = ref(false)
const savedOnce = ref(false)
const coreBlocked = ref(false)
let hydrating = false
let lastCore = ''
// Track the last slug/SKU we auto-generated so we only keep tracking the name while the field is
// still that value (or empty) — a merchant-customized slug/SKU is left untouched.
let lastAutoSlug = ''
let lastAutoSku = ''

const saveStatus = computed(() => {
  if (isCreate.value) return null
  if (saving.value > 0) return { label: 'Saving…', icon: 'o_sync', chip: 'blue-1', text: 'primary', spin: true }
  if (coreBlocked.value) return { label: 'Fix errors to save', icon: 'o_error_outline', chip: 'red-1', text: 'negative' }
  if (saveError.value) return { label: 'Couldn’t save — retry', icon: 'o_cloud_off', chip: 'red-1', text: 'negative' }
  if (savedOnce.value) return { label: 'All changes saved', icon: 'o_cloud_done', chip: 'green-1', text: 'positive' }
  return { label: 'Auto-save on', icon: 'o_cloud_queue', chip: 'grey-3', text: 'grey-8' }
})

async function runSave (fn) {
  saving.value++
  saveError.value = false
  try {
    await fn()
    savedOnce.value = true
  } catch (err) {
    saveError.value = true
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value--
  }
}

// ---- Slug / SKU derivation ----------------------------------------------------
// "Men's Red Polo Shirt" -> slug "mens-red-polo-shirt", SKU "MENS-RED-POLO-SHIRT".
function slugify (value) {
  return (value || '')
    .toString()
    .trim()
    .toLowerCase()
    .replace(/['’"]/g, '')       // drop apostrophes/quotes so "Men's" -> "mens"
    .replace(/[^a-z0-9]+/g, '-') // any other run of non-alphanumerics -> a single hyphen
    .replace(/^-+|-+$/g, '')     // trim leading/trailing hyphens
}
function skuify (value) {
  return slugify(value).toUpperCase()
}

// ---- Payload + hydration ------------------------------------------------------
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
    metaTitle: form.metaTitle || null,
    metaDescription: form.metaDescription || null,
    metaKeywords: form.metaKeywords || null,
    sku: form.sku || null,
    price: toNumberOrNull(form.price),
    stockQuantity: toNumberOrNull(form.stockQuantity) || 0,
    allowBackorder: form.allowBackorder,
    estimatedRestockDate: form.allowBackorder ? (form.estimatedRestockDate || null) : null,
    manufacturerId: form.manufacturerId || null,
    isPublished: form.isPublished,
    displayOrder: toNumberOrNull(form.displayOrder) || 0,
    isFeatured: form.isFeatured,
    featuredDisplayOrder: form.isFeatured ? (toNumberOrNull(form.featuredDisplayOrder) || 0) : 0
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

function hydrate (p) {
  hydrating = true
  // Reset the auto-trackers: a saved product's slug/SKU is treated as customized, so renaming an
  // existing product never rewrites its (URL-stable) slug — only an empty field auto-fills.
  lastAutoSlug = ''
  lastAutoSku = ''
  for (const k of CORE_KEYS) form[k] = p[k] ?? EMPTY[k]
  // Date-only for the <input type=date> (the API returns a full ISO timestamp).
  if (form.estimatedRestockDate) form.estimatedRestockDate = String(form.estimatedRestockDate).slice(0, 10)
  categoryIds.value = [...(p.categoryIds || [])]
  tagNames.value = (p.tags || []).map((t) => t.name)
  tiers.value = (p.tierPrices || []).filter((t) => !t.productVariantId).map((t) => ({ minQuantity: t.minQuantity, price: t.price }))
  attributeIds.value = [...(p.attributeIds || [])]
  variants.value = (p.variants || []).map((v) => ({
    id: v.id, sku: v.sku, price: v.price, stockQuantity: v.stockQuantity,
    allowBackorder: v.allowBackorder, isEnabled: v.isEnabled, displayOrder: v.displayOrder,
    attributeValueIds: v.attributeValueIds || [], _saving: false, _saved: false
  }))
  lastCore = JSON.stringify(buildPayload())
  nextTick(() => { hydrating = false })
}

// ---- Core auto-save -----------------------------------------------------------
const saveCore = debounce(async () => {
  if (isCreate.value) return
  const ok = await v$.value.$validate()
  if (!ok) { coreBlocked.value = true; return }
  coreBlocked.value = false
  const payload = buildPayload()
  const snapshot = JSON.stringify(payload)
  if (snapshot === lastCore) return
  await runSave(async () => {
    await productApi.update(pid.value, payload)
    lastCore = snapshot
  })
}, 800)

watch(form, () => { if (!hydrating && !isCreate.value) saveCore() }, { deep: true })

// Auto-generate the slug + SKU from the name while each field is still empty or unchanged from the
// value we last derived. Once the merchant types their own slug/SKU it stops tracking; clearing the
// field (then editing the name) resumes it. In edit mode this also triggers the core auto-save.
watch(() => form.name, (name) => {
  if (!form.slug || form.slug === lastAutoSlug) {
    lastAutoSlug = slugify(name)
    form.slug = lastAutoSlug
  }
  if (!form.sku || form.sku === lastAutoSku) {
    lastAutoSku = skuify(name)
    form.sku = lastAutoSku
  }
})

// If the type stops being variant-capable while that tab is open, fall back to General.
watch(isVariantType, (v) => { if (!v && tab.value === 'variants') tab.value = 'general' })

// ---- Sub-resource auto-save ---------------------------------------------------
const queueCategories = debounce(() => {
  if (hydrating) return
  runSave(async () => { product.value = await productApi.setCategories(pid.value, categoryIds.value) })
}, 700)

const queueTags = debounce(() => {
  if (hydrating) return
  runSave(async () => { product.value = await productApi.setTags(pid.value, tagNames.value) })
}, 700)

const queueTiers = debounce(() => {
  if (hydrating) return
  const payload = tiers.value
    .filter((t) => t.minQuantity != null && t.minQuantity !== '' && t.price != null && t.price !== '')
    .map((t) => ({ minQuantity: Number(t.minQuantity) || 0, price: Number(t.price) || 0 }))
  runSave(async () => { product.value = await productApi.setTierPrices(pid.value, payload) })
}, 800)

const queueAttributes = debounce(() => {
  if (hydrating) return
  runSave(async () => { product.value = await productApi.setAttributes(pid.value, attributeIds.value) })
}, 700)

function addTier () { tiers.value.push({ minQuantity: 1, price: 0 }) }
function removeTier (i) { tiers.value.splice(i, 1); queueTiers() }

// ---- Variants (per-row debounced save) ----------------------------------------
const variantSavers = {}
function queueVariant (variant) {
  if (!variantSavers[variant.id]) {
    variantSavers[variant.id] = debounce(async () => {
      variant._saving = true
      variant._saved = false
      try {
        await productApi.updateVariant(variant.id, {
          sku: variant.sku || null,
          price: variant.price === '' || variant.price === null ? null : Number(variant.price),
          stockQuantity: Number(variant.stockQuantity) || 0,
          allowBackorder: variant.allowBackorder,
          isEnabled: variant.isEnabled,
          displayOrder: Number(variant.displayOrder) || 0
        })
        savedOnce.value = true
        variant._saved = true
        setTimeout(() => { variant._saved = false }, 2000)
      } catch (err) {
        saveError.value = true
        notify.error(getApiErrorMessage(err))
      } finally {
        variant._saving = false
      }
    }, 700)
  }
  variantSavers[variant.id]()
}

// ---- Media (images via the centralized library) -------------------------------
async function loadPictures () {
  try {
    pictures.value = await productApi.listPictures(pid.value)
  } catch (err) { pictures.value = [] }
}

function openPicture (pic) {
  selectedMediaId.value = pic.mediaId || null
  lightboxUrl.value = pic.url
  lightboxOpen.value = true
}
async function onPictureSaved () {
  // Refresh so the new alt text / SEO-renamed URL is reflected in the grid.
  try { pictures.value = await productApi.listPictures(pid.value) } catch (e) { /* keep current */ }
}

// Each image runs the two-step media flow (prepare → commit) then is assigned as a ProductPicture.
// Alt text defaults to the product name; the SEO file name comes from the prepare step's suggestion.
async function onImageFiles (selection) {
  const files = Array.isArray(selection) ? selection : (selection ? [selection] : [])
  if (!files.length) return
  uploadingImages.value = true
  try {
    let added = 0
    for (const file of files) {
      const draft = await mediaApi.prepare(file)
      const committed = await mediaApi.commit({
        tempId: draft.tempId,
        seoFileName: draft.suggestedSeoFileName,
        altText: form.name || null,
        title: null,
        caption: null,
        description: null
      })
      await productApi.assignPicture(pid.value, { mediaId: committed.mediaId })
      added++
    }
    notify.success(added > 1 ? `${added} images added` : 'Image added')
    savedOnce.value = true
    await loadPictures()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    uploadingImages.value = false
    imageFiles.value = null
  }
}

async function removePicture (pic) {
  try {
    await productApi.removePicture(pic.id)
    pictures.value = pictures.value.filter((p) => p.id !== pic.id)
    notify.success('Image removed')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

async function addVideo () {
  const url = newVideoUrl.value.trim()
  if (!url) { notify.warning('Enter a video URL'); return }
  addingVideo.value = true
  try {
    await productApi.addVideo(pid.value, { url, altText: null })
    notify.success('Video added')
    newVideoUrl.value = ''
    await loadPictures()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    addingVideo.value = false
  }
}

async function removeImage (img) {
  if (!(await deleteConfirmation('this media entry'))) return
  try {
    await productApi.removePicture(img.id)
    notify.success('Media deleted')
    await loadPictures()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

// ---- Variant operations -------------------------------------------------------
function variantLabel (variant) {
  const parts = (variant.attributeValueIds || []).map((id) => attributeValueMap.value[id] || id)
  return parts.length ? parts.join(', ') : '(no attributes)'
}

async function generate () {
  generating.value = true
  try {
    product.value = await productApi.generateVariants(pid.value)
    hydrate(product.value)
    notify.success('Variants generated')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    generating.value = false
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

// ---- Create -------------------------------------------------------------------
const creating = ref(false)
async function createProduct () {
  const ok = await v$.value.$validate()
  if (!ok) { notify.warning('Fill in the required fields'); return }
  creating.value = true
  try {
    const created = await productApi.create(buildPayload())
    notify.success('Product created')
    router.replace({ name: 'catalog-product-detail', params: { id: created.id } })
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    creating.value = false
  }
}

// ---- Loading ------------------------------------------------------------------
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
  } catch (err) { categoryOptions.value = [] }
}

async function loadAttributes () {
  try {
    const result = await productAttributeApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    attributeOptions.value = items.map((a) => ({ label: a.name, value: a.id }))
    const map = {}
    for (const attr of items) {
      for (const val of attr.values || []) map[val.id] = `${attr.name}: ${val.value}`
    }
    attributeValueMap.value = map
  } catch (err) { attributeOptions.value = [] }
}

async function loadManufacturers () {
  try {
    const result = await manufacturerApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    manufacturerOptions.value = items.map((m) => ({ label: m.name, value: m.id }))
  } catch (err) { manufacturerOptions.value = [] }
}

async function loadTaxCategories () {
  try {
    const result = await taxCategoryApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(result) ? result : result?.items || []
    taxCategoryOptions.value = items.map((t) => ({ label: t.name, value: t.id }))
    // For a brand-new product, preselect the first tax category so the required field is satisfied.
    if (isCreate.value && !form.taxCategoryId && taxCategoryOptions.value.length) {
      hydrating = true
      form.taxCategoryId = taxCategoryOptions.value[0].value
      nextTick(() => { hydrating = false })
    }
  } catch (err) { taxCategoryOptions.value = [] }
}

async function load () {
  loading.value = true
  try {
    product.value = await productApi.get(pid.value)
    hydrate(product.value)
    loadPictures()
  } catch (err) {
    product.value = null
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function init () {
  // Reset transient state on (re)entry.
  saveError.value = false
  savedOnce.value = false
  coreBlocked.value = false
  tab.value = 'general'
  if (isCreate.value) {
    hydrating = true
    Object.assign(form, EMPTY)
    categoryIds.value = []; tagNames.value = []; tiers.value = []; attributeIds.value = []; variants.value = []; pictures.value = []
    product.value = null
    loading.value = false
    v$.value.$reset()
    await loadTaxCategories()
    nextTick(() => { hydrating = false })
    return
  }
  await load()
}

onMounted(() => {
  init()
  loadCategories()
  loadAttributes()
  loadManufacturers()
  if (!isCreate.value) loadTaxCategories()
})

// After create we router.replace to the detail route on the same component instance — reload on change.
watch(() => route.fullPath, () => { init(); if (!isCreate.value) loadTaxCategories() })
</script>

<style scoped lang="scss">
.app-product-tabs {
  :deep(.q-tab) { min-height: 44px; }
}

// Google-style search-result preview on the SEO tab.
.seo-preview {
  max-width: 600px;
  &__title { color: #1a0dab; font-size: 18px; line-height: 1.3; }
  &__url { color: #006621; font-size: 13px; margin: 2px 0 4px; }
  &__desc { color: #4d5156; font-size: 13px; line-height: 1.4; }
}

// Media-backed picture thumbnails.
.product-pic {
  position: relative;
  width: 92px;
  &__img {
    width: 92px;
    height: 92px;
    object-fit: cover;
    border: 1px solid rgba(0, 0, 0, 0.12);
    border-radius: 6px;
    cursor: pointer;
    background: #fafafa;
  }
  &__remove {
    position: absolute;
    top: -8px;
    right: -8px;
  }
}
</style>
