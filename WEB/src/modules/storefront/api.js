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
import { anonApi, unwrap, qsSerializer } from 'services/api'

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
  listingSortOptions,
  searchSortOptions,
  formatPrice,
  normalizeFacets,
  productImage,
  productRouteParam
}
