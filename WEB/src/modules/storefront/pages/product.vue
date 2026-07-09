<template>
  <q-page class="storefront-root">
    <div class="sf-container q-py-md">
      <q-inner-loading :showing="loading" color="primary" />

      <q-banner v-if="!loading && !product" class="bg-grey-2 rounded-borders q-my-md">
        This product was not found.
        <template #action>
          <q-btn flat no-caps color="primary" label="Back to shop" :to="{ name: 'shop-home' }" />
        </template>
      </q-banner>

      <template v-if="product">
        <!-- Breadcrumb (the product name is the H1 in the info column, so keep it compact here) -->
        <nav class="sf-crumbs" aria-label="Breadcrumb">
          <router-link class="sf-crumbs__link" :to="{ name: 'shop-home' }">
            <q-icon name="o_home" size="15px" /> Home
          </router-link>
          <q-icon name="o_chevron_right" size="16px" class="sf-crumbs__sep" />
          <span class="sf-crumbs__here">{{ product.name }}</span>
        </nav>

        <div class="row q-col-gutter-xl">
          <!-- Gallery -->
          <div class="col-12 col-md-7">
            <ProductGalleryView
              :images="galleryImages"
              :variant-id="variantId"
              :product-name="product.name"
            />
          </div>

          <!-- Info -->
          <div class="col-12 col-md-5">
            <h1 class="text-h4 text-weight-bold q-mb-xs" style="line-height: 1.2">{{ product.name }}</h1>

            <!-- Rating bar (no rating data yet — invites the first review) -->
            <div class="row items-center q-gutter-sm q-mb-md">
              <StarRating v-if="rating != null" :value="rating" :count="reviewCount" />
              <a v-else class="text-caption text-primary cursor-pointer" @click="tab = 'reviews'">Be the first to review</a>
            </div>

            <!-- Price block -->
            <div class="row items-baseline q-gutter-sm">
              <span class="text-h4 text-weight-bold" style="color: var(--sf-price)">{{ formatPrice(displayPrice) }}</span>
              <span v-if="oldPrice" class="text-h6 sf-card__price-old">{{ formatPrice(oldPrice) }}</span>
              <q-badge v-if="savings" color="red" :label="`Save ${formatPrice(savings)}`" class="q-py-xs q-px-sm" />
            </div>
            <div class="text-caption text-grey-6 q-mb-md">Price includes applicable taxes at checkout.</div>

            <div class="row items-center q-gutter-sm q-mb-md">
              <q-badge :color="availability.color" :label="availability.label" class="q-py-xs q-px-sm" />
              <span v-if="stockNote" class="text-caption row items-center" :class="stockNote.cls">
                <q-icon name="o_inventory_2" size="14px" class="q-mr-xs" />{{ stockNote.text }}
              </span>
            </div>
            <div v-if="restockNote" class="text-caption text-orange-9 q-mb-sm">{{ restockNote }}</div>

            <div v-if="product.shortDescription" class="text-body1 text-grey-8 q-mb-md">{{ product.shortDescription }}</div>

            <q-separator class="q-my-md" />

            <VariantSelector
              v-if="product.variants && product.variants.length"
              v-model="variantId"
              :variants="product.variants"
              :attributes="product.attributes"
              class="q-mb-md"
            />

            <!-- Quantity + actions -->
            <div class="row items-center q-gutter-md q-mb-md">
              <div class="sf-qty row items-center no-wrap">
                <q-btn flat dense icon="o_remove" :disable="quantity <= 1" @click="quantity = Math.max(1, quantity - 1)" />
                <input v-model.number="quantity" type="number" min="1" class="sf-qty__input">
                <q-btn flat dense icon="o_add" @click="quantity = quantity + 1" />
              </div>
              <button
                class="sf-btn sf-btn--primary col"
                :disabled="addingToCart || needsSelection || availability.color === 'grey'"
                @click="onAddToCart"
              >
                <q-icon v-if="!addingToCart" name="o_shopping_cart" size="18px" />
                <q-spinner v-else size="16px" />
                {{ needsSelection ? 'Select options' : 'Add to Cart' }}
              </button>
            </div>

            <div class="row q-gutter-sm q-mb-lg">
              <q-btn outline color="pink-6" icon="o_favorite_border" label="Wishlist" no-caps :loading="addingToWishlist" @click="onAddToWishlist" />
              <q-btn outline color="dark" :icon="inCompare ? 'o_compare_arrows' : 'o_compare'" :label="inCompare ? 'In compare' : 'Compare'" no-caps @click="onToggleCompare" />
            </div>

            <!-- Meta -->
            <q-list dense class="text-body2 sf-meta">
              <div v-if="product.sku" class="row"><span class="sf-meta__k">SKU</span><span>{{ product.sku }}</span></div>
              <div v-if="product.manufacturerId" class="row">
                <span class="sf-meta__k">Brand</span>
                <router-link :to="{ name: 'shop-search', query: { manufacturerId: product.manufacturerId } }" class="text-primary">View brand</router-link>
              </div>
              <div v-if="product.tagNames && product.tagNames.length" class="row">
                <span class="sf-meta__k">Tags</span>
                <span>
                  <q-chip v-for="tag in product.tagNames" :key="tag" dense outline size="sm" color="grey-7" :label="tag" />
                </span>
              </div>
            </q-list>

            <!-- Share -->
            <div class="row items-center q-gutter-sm q-mt-md">
              <span class="text-caption text-grey-7">Share:</span>
              <q-btn round dense flat size="sm" icon="fab fa-facebook-f" type="a" :href="shareUrl('facebook')" target="_blank" />
              <q-btn round dense flat size="sm" icon="fab fa-x-twitter" type="a" :href="shareUrl('twitter')" target="_blank" />
              <q-btn round dense flat size="sm" icon="fab fa-pinterest-p" type="a" :href="shareUrl('pinterest')" target="_blank" />
              <q-btn round dense flat size="sm" icon="o_link" @click="copyLink" />
            </div>
          </div>
        </div>

        <!-- ===== Tabs ===== -->
        <div class="q-mt-xl">
          <q-tabs v-model="tab" dense no-caps align="left" active-color="primary" indicator-color="primary" class="text-grey-7 sf-tabs">
            <q-tab name="description" label="Description" />
            <q-tab name="reviews" :label="`Reviews${reviewCount != null ? ' (' + reviewCount + ')' : ''}`" />
            <q-tab name="qa" label="Q&A" />
          </q-tabs>
          <q-separator />
          <q-tab-panels v-model="tab" animated class="bg-transparent q-mt-md">
            <!-- Description -->
            <q-tab-panel name="description" class="q-px-none">
              <!-- eslint-disable-next-line vue/no-v-html -->
              <div v-if="product.fullDescription" class="storefront-rich-text" v-html="product.fullDescription" />
              <div v-else class="text-grey-6">No description available.</div>
            </q-tab-panel>

            <!-- Reviews (no backend yet — graceful) -->
            <q-tab-panel name="reviews" class="q-px-none">
              <div class="row items-center q-mb-md">
                <div class="col">
                  <div class="text-h6">Customer Reviews</div>
                  <div class="text-grey-6 text-body2">Be the first to share your thoughts.</div>
                </div>
                <q-btn color="primary" no-caps unelevated label="Write a Review" @click="reviewForm = !reviewForm" />
              </div>
              <q-slide-transition>
                <q-card v-if="reviewForm" flat bordered class="q-mb-md">
                  <q-card-section class="q-gutter-sm">
                    <div class="row items-center q-gutter-sm">
                      <span class="text-body2">Your rating:</span>
                      <q-rating v-model="newReview.rating" size="24px" color="amber" />
                    </div>
                    <q-input v-model="newReview.title" dense outlined label="Title" />
                    <q-input v-model="newReview.body" dense outlined type="textarea" label="Your review" />
                  </q-card-section>
                  <q-card-actions align="right">
                    <q-btn flat no-caps label="Cancel" @click="reviewForm = false" />
                    <q-btn color="primary" unelevated no-caps label="Submit review" @click="submitReview" />
                  </q-card-actions>
                </q-card>
              </q-slide-transition>
              <div class="text-grey-6 q-py-lg text-center bg-grey-1 rounded-borders">
                <q-icon name="o_rate_review" size="36px" class="q-mb-sm" />
                <div>No reviews yet.</div>
              </div>
            </q-tab-panel>

            <!-- Q&A (no backend yet — graceful) -->
            <q-tab-panel name="qa" class="q-px-none">
              <div class="row items-center q-mb-md">
                <div class="col">
                  <div class="text-h6">Questions & Answers</div>
                  <div class="text-grey-6 text-body2">Ask about this product.</div>
                </div>
                <q-btn color="primary" no-caps unelevated label="Ask a Question" @click="qaForm = !qaForm" />
              </div>
              <q-slide-transition>
                <q-card v-if="qaForm" flat bordered class="q-mb-md">
                  <q-card-section>
                    <q-input v-model="newQuestion" dense outlined type="textarea" label="Your question" />
                  </q-card-section>
                  <q-card-actions align="right">
                    <q-btn flat no-caps label="Cancel" @click="qaForm = false" />
                    <q-btn color="primary" unelevated no-caps label="Submit question" @click="submitQuestion" />
                  </q-card-actions>
                </q-card>
              </q-slide-transition>
              <div class="text-grey-6 q-py-lg text-center bg-grey-1 rounded-borders">
                <q-icon name="o_forum" size="36px" class="q-mb-sm" />
                <div>No questions yet.</div>
              </div>
            </q-tab-panel>
          </q-tab-panels>
        </div>

        <!-- Relationship carousels -->
        <div v-for="section in sections" :key="section.title" class="sf-section">
          <div class="sf-section__head"><h2 class="sf-section__title">{{ section.title }}</h2></div>
          <ProductCarousel :products="section.products" />
        </div>

        <div class="q-mt-xl">
          <RecentlyViewed :exclude-id="product.id" />
        </div>
      </template>
    </div>
  </q-page>
</template>

<script setup>
/*
 * Porto storefront product detail (WO-111, builds on WO-19). Gallery (reused
 * ProductGalleryView) + info column (title, price block with savings, variant
 * selectors, quantity, add-to-cart / wishlist / compare, meta, share), a
 * Description / Reviews / Q&A tab set, and Related / Cross-sell / Up-sell rails.
 *
 * Deferred (no backend yet, flagged on WO-111): Reviews (REQ-CAT-010) and Q&A
 * (REQ-CNT-007) render graceful empty states + forms that acknowledge "coming
 * soon"; star rating shows only when a rating field is present. The DTO carries
 * no category (so no category breadcrumb) and only a manufacturerId (no name),
 * and variant option labels are ids only (VariantSelector labels by SKU + price).
 */
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useQuasar } from 'quasar'
import { storefrontApi, wishlistApi, formatPrice } from 'modules/storefront/api'
import { useRecentlyViewed, useCompare } from 'modules/storefront/composables/useStorefrontStorage'
import { useCart } from 'modules/storefront/composables/useCart'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'
import ProductGalleryView from 'modules/catalog/components/ProductGalleryView.vue'
import VariantSelector from 'modules/storefront/components/VariantSelector.vue'
import ProductCarousel from 'modules/storefront/components/ProductCarousel.vue'
import RecentlyViewed from 'modules/storefront/components/RecentlyViewed.vue'
import StarRating from 'modules/storefront/components/StarRating.vue'

const route = useRoute()
const $q = useQuasar()
const notify = useNotify()
const { record } = useRecentlyViewed()
const { has, toggle, max } = useCompare()
const { addItem } = useCart()

const product = ref(null)
const loading = ref(false)
const variantId = ref(null)
const quantity = ref(1)
const addingToCart = ref(false)
const addingToWishlist = ref(false)
const tab = ref('description')

// Review / Q&A local form state (no backend yet).
const reviewForm = ref(false)
const qaForm = ref(false)
const newReview = ref({ rating: 5, title: '', body: '' })
const newQuestion = ref('')

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

// Defensive (no sale/rating fields in the DTO yet).
const oldPrice = computed(() => product.value?.oldPrice || product.value?.originalPrice || null)
const savings = computed(() => (oldPrice.value && displayPrice.value ? oldPrice.value - displayPrice.value : null))
const rating = computed(() => {
  const r = product.value?.averageRating ?? product.value?.rating
  return typeof r === 'number' ? r : null
})
const reviewCount = computed(() => product.value?.reviewCount ?? null)

const hasVariants = computed(() => (product.value?.variants || []).length > 0)

const stock = computed(() =>
  selectedVariant.value ? selectedVariant.value.stockQuantity : (product.value?.stockQuantity ?? 0)
)

// For variant products, availability/stock only make sense once a variant is chosen.
const needsSelection = computed(() => hasVariants.value && !selectedVariant.value)

const availability = computed(() => {
  if (needsSelection.value) return { label: 'Select options', color: 'grey-6' }
  if (stock.value > 0) return { label: 'In stock', color: 'positive' }
  if (product.value?.allowBackorder) return { label: 'Available on backorder', color: 'warning' }
  return { label: 'Out of stock', color: 'grey' }
})

// The available-quantity hint shown next to the badge (once a variant is picked, or for simple products).
const stockNote = computed(() => {
  if (needsSelection.value || stock.value <= 0) return null
  return stock.value <= 5
    ? { text: `Only ${stock.value} left`, cls: 'text-orange-9' }
    : { text: `${stock.value} in stock`, cls: 'text-grey-7' }
})
const restockNote = computed(() => {
  if (stock.value > 0 || !product.value?.allowBackorder || !product.value?.estimatedRestockDate) return null
  return `Estimated restock: ${new Date(product.value.estimatedRestockDate).toLocaleDateString()}`
})

const inCompare = computed(() => (product.value ? has(product.value.id) : false))

const sections = computed(() =>
  [
    { title: 'Related Products', products: product.value?.relatedProducts || [] },
    { title: 'Customers Also Bought', products: product.value?.crossSells || [] },
    { title: 'You May Also Like', products: product.value?.upSells || [] }
  ].filter((s) => s.products.length)
)

function shareUrl (network) {
  const url = typeof window !== 'undefined' ? encodeURIComponent(window.location.href) : ''
  const text = encodeURIComponent(product.value?.name || '')
  switch (network) {
    case 'facebook': return `https://www.facebook.com/sharer/sharer.php?u=${url}`
    case 'twitter': return `https://twitter.com/intent/tweet?url=${url}&text=${text}`
    case 'pinterest': return `https://pinterest.com/pin/create/button/?url=${url}&description=${text}`
    default: return '#'
  }
}

async function copyLink () {
  try {
    await navigator.clipboard.writeText(window.location.href)
    notify.success('Link copied')
  } catch (e) {
    notify.info(window.location.href)
  }
}

async function load () {
  loading.value = true
  variantId.value = null
  quantity.value = 1
  tab.value = 'description'
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
    await addItem({ productId: product.value.id, productVariantId: variantId.value || null, quantity: Math.max(1, quantity.value || 1) })
    notify.success('Added to cart')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    addingToCart.value = false
  }
}

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

function onToggleCompare () {
  if (!product.value) return
  const result = toggle(product.value.id)
  if (result.full) notify.warning('You can compare up to ' + max + ' products.')
  else if (result.removed) notify.info('Removed from compare')
  else notify.success('Added to compare')
}

// Reviews / Q&A have no backend yet — acknowledge and reset the form.
function submitReview () {
  reviewForm.value = false
  newReview.value = { rating: 5, title: '', body: '' }
  notify.info('Thanks! Product reviews are coming soon.')
}
function submitQuestion () {
  qaForm.value = false
  newQuestion.value = ''
  notify.info('Thanks! Product Q&A is coming soon.')
}

watch(() => route.params.idOrSlug, load)
onMounted(load)
</script>

<style scoped lang="scss">
.storefront-rich-text {
  :deep(img) { max-width: 100%; height: auto; }
}
.sf-qty {
  border: 1px solid var(--sf-border);
  border-radius: var(--sf-radius);
}
.sf-qty__input {
  width: 48px;
  border: none;
  text-align: center;
  font-size: 15px;
  outline: none;
  -moz-appearance: textfield;
}
.sf-qty__input::-webkit-outer-spin-button,
.sf-qty__input::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
.sf-meta__k {
  display: inline-block;
  width: 90px;
  color: var(--sf-muted);
  text-transform: uppercase;
  font-size: 11px;
  letter-spacing: 0.4px;
}
.sf-tabs { border-bottom: 1px solid var(--sf-border); }
</style>
