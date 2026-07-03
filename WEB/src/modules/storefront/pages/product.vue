<template>
  <q-page class="q-pa-md storefront-container">
    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !product" class="bg-grey-2 rounded-borders q-my-md">
      This product was not found.
      <template #action>
        <q-btn flat no-caps color="primary" label="Back to shop" :to="{ name: 'shop-home' }" />
      </template>
    </q-banner>

    <template v-if="product">
      <div class="row q-col-gutter-xl">
        <!-- Gallery -->
        <div class="col-12 col-md-6">
          <ProductGalleryView
            :images="galleryImages"
            :variant-id="variantId"
            :product-name="product.name"
          />
        </div>

        <!-- Info -->
        <div class="col-12 col-md-6">
          <div class="text-h4 text-weight-bold">{{ product.name }}</div>

          <div class="row items-center q-gutter-sm q-mt-sm">
            <div class="text-h5 text-primary text-weight-bold">{{ formatPrice(displayPrice) }}</div>
            <q-badge :color="availability.color" :label="availability.label" class="q-py-xs q-px-sm" />
          </div>

          <div v-if="product.sku" class="text-caption text-grey-7 q-mt-xs">SKU: {{ product.sku }}</div>

          <div v-if="product.shortDescription" class="text-body1 q-mt-md">
            {{ product.shortDescription }}
          </div>

          <q-separator class="q-my-md" />

          <VariantSelector
            v-if="product.variants && product.variants.length"
            v-model="variantId"
            :variants="product.variants"
            class="q-mb-md"
          />

          <div class="row q-gutter-sm q-mt-md">
            <q-btn
              unelevated
              color="primary"
              icon="o_shopping_cart"
              label="Add to cart"
              no-caps
              :loading="addingToCart"
              :disable="availability.color === 'grey'"
              @click="onAddToCart"
            />
            <q-btn
              outline
              color="primary"
              :icon="inCompare ? 'o_compare_arrows' : 'o_compare'"
              :label="inCompare ? 'In compare' : 'Compare'"
              no-caps
              @click="onToggleCompare"
            />
            <q-btn
              outline
              color="pink-6"
              icon="o_favorite_border"
              label="Wishlist"
              no-caps
              :loading="addingToWishlist"
              @click="onAddToWishlist"
            />
          </div>

          <div v-if="product.tagNames && product.tagNames.length" class="q-mt-lg">
            <q-chip
              v-for="tag in product.tagNames"
              :key="tag"
              dense
              outline
              color="grey-7"
              :label="tag"
            />
          </div>
        </div>
      </div>

      <!-- Full description -->
      <q-card v-if="product.fullDescription" flat bordered class="q-mt-xl">
        <q-card-section>
          <div class="text-h6 q-mb-sm">Description</div>
          <!-- eslint-disable-next-line vue/no-v-html -->
          <div class="storefront-rich-text" v-html="product.fullDescription" />
        </q-card-section>
      </q-card>

      <!-- Relationship sections -->
      <section v-for="section in sections" :key="section.title" class="q-mt-xl">
        <div class="text-h6 q-mb-md">{{ section.title }}</div>
        <div class="row q-col-gutter-md">
          <div
            v-for="p in section.products"
            :key="p.id"
            class="col-6 col-sm-4 col-md-3 col-lg-2"
          >
            <ProductCard :product="p" />
          </div>
        </div>
      </section>

      <div class="q-mt-xl">
        <RecentlyViewed :exclude-id="product.id" />
      </div>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Storefront product detail (WO-19, AC-CAT-007.2, PZG). Left: the reused
 * ProductGalleryView (product- and variant-level media, driven by the selected
 * variant). Right: name, live price (variant price overrides the product price),
 * availability, description and a VariantSelector. Below: related / cross-sell /
 * up-sell rows plus recently-viewed. On load the product is recorded into the
 * recently-viewed list.
 *
 * NOTE: StorefrontImageDto carries no `id`, but ProductGalleryView keys slides by
 * `img.id`, so a stable synthetic id is injected here.
 */
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { storefrontApi, wishlistApi, formatPrice } from 'modules/storefront/api'
import { useRecentlyViewed, useCompare } from 'modules/storefront/composables/useStorefrontStorage'
import { useCart } from 'modules/storefront/composables/useCart'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'
import ProductGalleryView from 'modules/catalog/components/ProductGalleryView.vue'
import ProductCard from 'modules/storefront/components/ProductCard.vue'
import VariantSelector from 'modules/storefront/components/VariantSelector.vue'
import RecentlyViewed from 'modules/storefront/components/RecentlyViewed.vue'

const route = useRoute()
const notify = useNotify()
const { record } = useRecentlyViewed()
const { has, toggle, max } = useCompare()
const { addItem } = useCart()

const product = ref(null)
const loading = ref(false)
const variantId = ref(null)
const addingToCart = ref(false)
const addingToWishlist = ref(false)

// ProductGalleryView requires a stable per-image id (the DTO omits one).
const galleryImages = computed(() =>
  (product.value?.images || []).map((img, i) => ({ ...img, id: 'img-' + i }))
)

const selectedVariant = computed(
  () => (product.value?.variants || []).find((v) => v.id === variantId.value) || null
)

const displayPrice = computed(() => {
  if (selectedVariant.value && selectedVariant.value.price != null) return selectedVariant.value.price
  return product.value?.price
})

const stock = computed(() =>
  selectedVariant.value ? selectedVariant.value.stockQuantity : (product.value?.stockQuantity ?? 0)
)

const availability = computed(() => {
  if (stock.value > 0) return { label: 'In stock', color: 'positive' }
  if (product.value?.allowBackorder) return { label: 'Available on backorder', color: 'warning' }
  return { label: 'Out of stock', color: 'grey' }
})

const inCompare = computed(() => (product.value ? has(product.value.id) : false))

// Save the current product/variant to the authenticated customer's wishlist (WO-29). The wishlist is
// registered-buyers-only, so an unauthenticated shopper is nudged to sign in rather than erroring.
async function onAddToWishlist () {
  if (!product.value) return
  addingToWishlist.value = true
  try {
    await wishlistApi.addItem({ productId: product.value.id, productVariantId: variantId.value || null })
    notify.success('Saved to your wishlist')
  } catch (err) {
    if (err?.response?.status === 401) notify.warning('Sign in to save items to your wishlist')
    else notify.error(getApiErrorMessage(err))
  } finally {
    addingToWishlist.value = false
  }
}

const sections = computed(() =>
  [
    { title: 'Related products', products: product.value?.relatedProducts || [] },
    { title: 'Customers also bought', products: product.value?.crossSells || [] },
    { title: 'You may also like', products: product.value?.upSells || [] }
  ].filter((s) => s.products.length)
)

async function load () {
  loading.value = true
  variantId.value = null
  try {
    product.value = await storefrontApi.productDetail(route.params.idOrSlug)
    if (product.value?.id) record(product.value.id)
  } catch (err) {
    product.value = null
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function onAddToCart () {
  if (!product.value || addingToCart.value) return
  addingToCart.value = true
  try {
    await addItem({ productId: product.value.id, productVariantId: variantId.value || null, quantity: 1 })
    notify.success('Added to cart')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    addingToCart.value = false
  }
}

function onToggleCompare () {
  if (!product.value) return
  const result = toggle(product.value.id)
  if (result.full) notify.warning('You can compare up to ' + max + ' products.')
  else if (result.removed) notify.info('Removed from compare')
  else if (result.ok) notify.success('Added to compare')
}

watch(() => route.params.idOrSlug, load)
onMounted(load)
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1400px;
  margin: 0 auto;
}

.storefront-rich-text {
  :deep(img) {
    max-width: 100%;
    height: auto;
  }
}
</style>
