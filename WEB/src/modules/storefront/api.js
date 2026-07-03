/*
 * Storefront module API layer (WO-19).
 *
 * Wraps the PUBLIC storefront endpoints exposed by StorefrontCatalogController
 * (/api/storefront/catalog/*) and ProductSearchController (/api/storefront/search).
 * Uses the ANONYMOUS axios instance (`anonApi`) — these routes are
 * [AllowAnonymous] and must not trigger the authenticated instance's 401
 * refresh interceptor.
 *
 * JSON is camelCase and enums serialize as string names (ProductMediaType =
 * Image|Video, ProductType = Simple|Grouped|WithVariants|Downloadable|GiftCard).
 */
import { api, anonApi, unwrap, qsSerializer } from 'services/api'

const CATALOG = '/api/storefront/catalog'
const SEARCH = '/api/storefront/search'

export const storefrontApi = {
  // Category landing page: meta + a page of published products + filterable specs.
  categoryPage (idOrSlug, params = {}) {
    return anonApi
      .get(CATALOG + '/category/' + encodeURIComponent(idOrSlug), { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Full published product detail (variants, media, specs, relationship sections).
  productDetail (idOrSlug) {
    return anonApi.get(CATALOG + '/product/' + encodeURIComponent(idOrSlug)).then(unwrap)
  },
  // Resolve a client-maintained list of product ids to summaries (published only, order preserved).
  recentlyViewed (productIds) {
    return anonApi.post(CATALOG + '/recently-viewed', productIds || []).then(unwrap)
  },
  // Side-by-side comparison (specs + prices) for the given product ids.
  compare (productIds) {
    return anonApi.post(CATALOG + '/compare', productIds || []).then(unwrap)
  },
  // Paged published products sharing a tag (resolved by slug or name).
  byTag (tagSlug, params = {}) {
    return anonApi
      .get(CATALOG + '/tag/' + encodeURIComponent(tagSlug), { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Paged published products for an enabled manufacturer (by id or slug).
  byManufacturer (idOrSlug, params = {}) {
    return anonApi
      .get(CATALOG + '/manufacturer/' + encodeURIComponent(idOrSlug), { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Faceted keyword search. Array filters (specificationOptionIds) bind from repeated query params.
  search (params = {}) {
    return anonApi.get(SEARCH, { params, paramsSerializer: qsSerializer }).then(unwrap)
  },
  // Autocomplete suggestions: matching product + category names.
  autocomplete (query, limit = 8) {
    return anonApi
      .get(SEARCH + '/autocomplete', { params: { query, limit }, paramsSerializer: qsSerializer })
      .then(unwrap)
  }
}

// ---- cart resource group (WO-28) --------------------------------------------
// A guest cart is keyed by a client-generated session id (persisted in
// localStorage under 'storefront.cartSession' by useCart), sent as the
// `sessionId` query on every call — the backend also accepts an X-Cart-Session
// header. Every mutation returns the recalculated CartDto.
const CART = '/api/cart'

export const cartApi = {
  // Get (or lazily create) the guest cart for this session.
  get (sessionId) {
    return anonApi.get(CART, { params: { sessionId }, paramsSerializer: qsSerializer }).then(unwrap)
  },
  // Add a product/variant (increments the line if it already exists).
  addItem (sessionId, { productId, productVariantId = null, quantity = 1 }) {
    return anonApi
      .post(CART + '/items', { productId, productVariantId, quantity }, { params: { sessionId }, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Set a line's quantity (0 removes it).
  updateItem (sessionId, itemId, quantity) {
    return anonApi
      .put(CART + '/items/' + encodeURIComponent(itemId), { quantity }, { params: { sessionId }, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Remove a single line.
  removeItem (sessionId, itemId) {
    return anonApi
      .delete(CART + '/items/' + encodeURIComponent(itemId), { params: { sessionId }, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  // Empty the cart.
  clear (sessionId) {
    return anonApi.delete(CART, { params: { sessionId }, paramsSerializer: qsSerializer }).then(unwrap)
  },
  // Coupons are keyed by the cart id (see CartCouponController), not the session id.
  // Returns ApplyCouponResult — re-fetch the cart to reflect the change.
  applyCoupon (cartId, code) {
    return anonApi.post(CART + '/apply-coupon', { cartId, code }).then(unwrap)
  },
  // DELETE with a body — RemoveCouponCommand binds { cartId } from the request body.
  removeCoupon (cartId) {
    return anonApi.delete(CART + '/remove-coupon', { data: { cartId } }).then(unwrap)
  }
}

// ---- wishlist resource group (WO-29) ----------------------------------------
// The wishlist belongs to an authenticated customer, so these use the AUTHENTICATED
// `api` instance (a bearer token from customer login). Every mutation returns the
// recalculated WishlistDto.
const WISHLIST = '/api/wishlist'

export const wishlistApi = {
  get () {
    return api.get(WISHLIST).then(unwrap)
  },
  addItem ({ productId, productVariantId = null }) {
    return api.post(WISHLIST + '/items', { productId, productVariantId }).then(unwrap)
  },
  removeItem (itemId) {
    return api.delete(WISHLIST + '/items/' + encodeURIComponent(itemId)).then(unwrap)
  },
  moveToCart (itemId, quantity = 1) {
    return api.post(WISHLIST + '/items/' + encodeURIComponent(itemId) + '/move-to-cart', { itemId, quantity }).then(unwrap)
  }
}

// ---- checkout resource group (WO-30) ----------------------------------------
const CHECKOUT = '/api/checkout'

export const checkoutApi = {
  // Read-only price preview for a delivery address. Returns CheckoutQuote
  // (subtotal, discounts, shippingOptions, tax, total, isRoutable, guestOrderingAllowed).
  quote (payload) {
    return anonApi.post(CHECKOUT + '/quote', payload).then(unwrap)
  },
  // Finalize the order + authorize payment. Returns CheckoutResult — a declined
  // payment comes back with success=false and a retryable pending order.
  place (payload) {
    return anonApi.post(CHECKOUT + '/place', payload).then(unwrap)
  }
}

// ---- currency resource group (WO-26) ----------------------------------------
export const currencyApi = {
  // Enabled display currencies (base first), each with code, symbol and rate.
  list () {
    return anonApi.get('/api/storefront/currencies').then(unwrap)
  }
}

// ---- Shared display helpers -------------------------------------------------

// Sort options for curated listing pages (category / tag / manufacturer). A blank
// value maps to the backend's curated order (display order, then name).
export const listingSortOptions = [
  { label: 'Featured', value: '' },
  { label: 'Price: Low to High', value: 'price_asc' },
  { label: 'Price: High to Low', value: 'price_desc' },
  { label: 'Name: A to Z', value: 'name_asc' },
  { label: 'Name: Z to A', value: 'name_desc' },
  { label: 'Newest', value: 'newest' }
]

// Sort options for keyword search — relevance is the natural default there.
export const searchSortOptions = [
  { label: 'Relevance', value: 'relevance' },
  ...listingSortOptions.slice(1)
]

// The storefront DTOs do not carry a currency; USD is assumed for display.
const CURRENCY = 'USD'

export function formatPrice (value, currency = CURRENCY) {
  if (value === null || value === undefined || value === '') return '—'
  const n = Number(value)
  if (Number.isNaN(n)) return '—'
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(n)
  } catch (e) {
    return n.toFixed(2)
  }
}

/*
 * Normalise the two backend facet shapes into one:
 *   category `filters`: { specificationAttributeId, name, options:[{ specificationAttributeOptionId, value }] }
 *   search   `facets` : { specificationAttributeId, name, values:[{ optionId, value, count }] }
 * -> { attributeId, name, options:[{ optionId, value, count|null }] }
 */
export function normalizeFacets (list) {
  if (!Array.isArray(list)) return []
  return list
    .map((g) => ({
      attributeId: g.specificationAttributeId,
      name: g.name || '',
      options: (g.values || g.options || [])
        .map((o) => ({
          optionId: o.optionId ?? o.specificationAttributeOptionId,
          value: o.value || '',
          count: typeof o.count === 'number' ? o.count : null
        }))
        .filter((o) => o.optionId)
    }))
    .filter((g) => g.attributeId && g.options.length)
}

// The primary image field differs by endpoint (summary=primaryImageUrl, search=imageUrl).
export function productImage (product) {
  if (!product) return null
  return product.primaryImageUrl || product.imageUrl || null
}

// Prefer a human-friendly slug for routing, falling back to the id.
export function productRouteParam (product) {
  return product ? (product.slug || product.id) : ''
}

export default {
  storefrontApi,
  cartApi,
  wishlistApi,
  checkoutApi,
  currencyApi,
  listingSortOptions,
  searchSortOptions,
  formatPrice,
  normalizeFacets,
  productImage,
  productRouteParam
}
