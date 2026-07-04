/*
 * useCategories (WO-109) — the public storefront category tree.
 *
 * Loads the enabled category tree once (module-singleton) from
 * GET /api/storefront/catalog/categories and shares it across the header
 * mega-menu and the home-page category grid.
 */
import { ref } from 'vue'
import { storefrontApi } from 'modules/storefront/api'

const categories = ref([])
const loaded = ref(false)
const loading = ref(false)

export function useCategories () {
  async function loadCategories (force = false) {
    if (loaded.value && !force) return categories.value
    if (loading.value) return categories.value
    loading.value = true
    try {
      const tree = await storefrontApi.categories()
      categories.value = Array.isArray(tree) ? tree : []
    } catch (e) {
      categories.value = []
    } finally {
      loaded.value = true
      loading.value = false
    }
    return categories.value
  }

  return { categories, loaded, loading, loadCategories }
}
