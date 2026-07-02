/*
 * Catalog module API layer (WO-15).
 *
 * Wraps the admin catalog endpoints exposed by AdminProductsController,
 * AdminCategoriesController, AdminManufacturersController and
 * AdminProductAttributesController. Mirrors the widgetApi style: uses the
 * authenticated `api` instance, `qsSerializer` for query strings and unwraps
 * every response with `unwrap`.
 *
 * Enums are transported as their string names (the API registers a
 * JsonStringEnumConverter): ProductType = Simple|Grouped|WithVariants|
 * Downloadable|GiftCard, GiftCardType = Fixed|OpenAmount, ProductMediaType =
 * Image|Video.
 */
import { api, unwrap, qsSerializer } from 'services/api'

// ---- Products ---------------------------------------------------------------
export const productApi = {
  // Core CRUD
  list (params = {}) {
    return api
      .get('/api/admin/products', { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  get (id) {
    return api.get(`/api/admin/products/${id}`).then(unwrap)
  },
  create (payload) {
    return api.post('/api/admin/products', payload).then(unwrap)
  },
  update (id, payload) {
    return api.put(`/api/admin/products/${id}`, payload).then(unwrap)
  },
  remove (id) {
    return api.delete(`/api/admin/products/${id}`).then(unwrap)
  },

  // Variants
  generateVariants (id) {
    return api.post(`/api/admin/products/${id}/variants/generate`).then(unwrap)
  },
  updateVariant (variantId, payload) {
    return api.put(`/api/admin/products/variants/${variantId}`, payload).then(unwrap)
  },
  deleteVariant (variantId) {
    return api.delete(`/api/admin/products/variants/${variantId}`).then(unwrap)
  },

  // Assignments (replace-semantics)
  setCategories (id, categoryIds) {
    return api.put(`/api/admin/products/${id}/categories`, { categoryIds }).then(unwrap)
  },
  setAttributes (id, productAttributeIds) {
    return api.put(`/api/admin/products/${id}/attributes`, { productAttributeIds }).then(unwrap)
  },
  setTags (id, tagNames) {
    return api.put(`/api/admin/products/${id}/tags`, { tagNames }).then(unwrap)
  },
  setTierPrices (id, tiers, productVariantId = null) {
    return api.put(`/api/admin/products/${id}/tier-prices`, { productVariantId, tiers }).then(unwrap)
  },

  // Media
  addImage (id, payload) {
    return api.post(`/api/admin/products/${id}/images`, payload).then(unwrap)
  },
  deleteImage (imageId) {
    return api.delete(`/api/admin/products/images/${imageId}`).then(unwrap)
  }
}

// ---- Categories -------------------------------------------------------------
export const categoryApi = {
  list (params = {}) {
    return api
      .get('/api/admin/categories', { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  tree () {
    return api.get('/api/admin/categories/tree').then(unwrap)
  },
  get (id) {
    return api.get(`/api/admin/categories/${id}`).then(unwrap)
  },
  create (payload) {
    return api.post('/api/admin/categories', payload).then(unwrap)
  },
  update (id, payload) {
    return api.put(`/api/admin/categories/${id}`, payload).then(unwrap)
  },
  remove (id) {
    return api.delete(`/api/admin/categories/${id}`).then(unwrap)
  }
}

// ---- Manufacturers ----------------------------------------------------------
export const manufacturerApi = {
  list (params = {}) {
    return api
      .get('/api/admin/manufacturers', { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  get (id) {
    return api.get(`/api/admin/manufacturers/${id}`).then(unwrap)
  },
  create (payload) {
    return api.post('/api/admin/manufacturers', payload).then(unwrap)
  },
  update (id, payload) {
    return api.put(`/api/admin/manufacturers/${id}`, payload).then(unwrap)
  },
  remove (id) {
    return api.delete(`/api/admin/manufacturers/${id}`).then(unwrap)
  }
}

// ---- Product attributes (read-only helper for variant generation) -----------
export const productAttributeApi = {
  list (params = {}) {
    return api
      .get('/api/admin/product-attributes', { params, paramsSerializer: qsSerializer })
      .then(unwrap)
  },
  get (id) {
    return api.get(`/api/admin/product-attributes/${id}`).then(unwrap)
  }
}

// ---- Shared enum option catalogs --------------------------------------------
export const productTypeOptions = [
  { label: 'Simple', value: 'Simple' },
  { label: 'Grouped', value: 'Grouped' },
  { label: 'With variants', value: 'WithVariants' },
  { label: 'Downloadable', value: 'Downloadable' },
  { label: 'Gift card', value: 'GiftCard' }
]

export const giftCardTypeOptions = [
  { label: 'Fixed amount', value: 'Fixed' },
  { label: 'Open amount', value: 'OpenAmount' }
]

export const mediaTypeOptions = [
  { label: 'Image', value: 'Image' },
  { label: 'Video', value: 'Video' }
]

export function productTypeLabel (value) {
  const match = productTypeOptions.find((o) => o.value === value)
  return match ? match.label : value || '—'
}

export default {
  productApi,
  categoryApi,
  manufacturerApi,
  productAttributeApi,
  productTypeOptions,
  giftCardTypeOptions,
  mediaTypeOptions,
  productTypeLabel
}
