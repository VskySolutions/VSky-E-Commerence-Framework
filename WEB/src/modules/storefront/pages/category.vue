<template>
  <q-page class="q-pa-md storefront-container">
    <q-banner v-if="!loading && notFound" class="bg-grey-2 rounded-borders q-my-md">
      This category was not found.
      <template #action>
        <q-btn flat no-caps color="primary" label="Back to shop" :to="{ name: 'shop-home' }" />
      </template>
    </q-banner>

    <template v-if="!notFound">
      <!-- Category header -->
      <div class="q-mb-md">
        <div class="text-h5 text-weight-bold">{{ category.name || 'Category' }}</div>
        <div v-if="category.description" class="text-body2 text-grey-7 q-mt-xs">
          {{ category.description }}
        </div>
      </div>

      <div class="row q-col-gutter-lg">
        <!-- Filters -->
        <div class="col-12 col-md-3">
          <q-card flat bordered class="q-pa-md">
            <FilterPanel
              :facets="facetGroups"
              :model-value="selectedOptionIds"
              @update:model-value="onFiltersChange"
            />
          </q-card>
        </div>

        <!-- Results -->
        <div class="col-12 col-md-9">
          <div class="row items-center q-mb-md">
            <div class="text-body2 text-grey-7 col">
              <span class="text-weight-medium text-dark">{{ totalCount }}</span>
              {{ totalCount === 1 ? 'product' : 'products' }}
            </div>
            <q-select
              v-model="sort"
              dense
              outlined
              emit-value
              map-options
              :options="sortOptions"
              label="Sort by"
              style="min-width: 200px"
              @update:model-value="onSortChange"
            />
          </div>

          <q-inner-loading :showing="loading" color="primary" />

          <div v-if="!loading && !items.length" class="text-grey-6 q-py-xl text-center">
            No products match your selection.
          </div>

          <div class="row q-col-gutter-md">
            <div
              v-for="p in items"
              :key="p.id"
              class="col-6 col-sm-4 col-md-4 col-lg-3"
            >
              <ProductCard :product="p" />
            </div>
          </div>

          <div v-if="totalPages > 1" class="row justify-center q-mt-lg">
            <q-pagination
              v-model="page"
              :max="totalPages"
              :max-pages="7"
              boundary-numbers
              direction-links
              @update:model-value="onPageChange"
            />
          </div>
        </div>
      </div>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Storefront category page (WO-19, AC-STF-003). Loads an enabled category by id
 * or slug (meta + first page of published products + filterable spec attributes)
 * from the catalog endpoint. Because that endpoint does not accept spec filters,
 * an active facet selection re-queries through the faceted search endpoint scoped
 * to the resolved categoryId (which also returns per-option counts). Sort,
 * pagination and a live match count are always shown.
 */
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { storefrontApi, normalizeFacets, listingSortOptions } from 'modules/storefront/api'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'
import ProductCard from 'modules/storefront/components/ProductCard.vue'
import FilterPanel from 'modules/storefront/components/FilterPanel.vue'

const route = useRoute()
const notify = useNotify()

const PAGE_SIZE = 12
const sortOptions = listingSortOptions

const category = ref({})
const categoryId = ref(null)
const items = ref([])
const totalCount = ref(0)
const page = ref(1)
const sort = ref('')
const selectedOptionIds = ref([])
const baseFacets = ref([])
const searchFacets = ref([])
const loading = ref(false)
const notFound = ref(false)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)))

// The full option universe from the category (stable), enriched with live counts
// from the faceted search once filters are applied.
const facetGroups = computed(() => {
  if (!searchFacets.value.length) return baseFacets.value
  const counts = {}
  for (const g of searchFacets.value) {
    for (const o of g.options) counts[o.optionId] = o.count
  }
  return baseFacets.value.map((g) => ({
    ...g,
    options: g.options.map((o) => ({ ...o, count: counts[o.optionId] ?? 0 }))
  }))
})

// Initial (and route-change) load: resolves the category and its first page.
async function loadCategory () {
  loading.value = true
  notFound.value = false
  try {
    const res = await storefrontApi.categoryPage(route.params.idOrSlug, {
      page: page.value,
      pageSize: PAGE_SIZE,
      sort: sort.value || undefined
    })
    category.value = res || {}
    categoryId.value = res?.categoryId || null
    baseFacets.value = normalizeFacets(res?.filters)
    searchFacets.value = []
    items.value = Array.isArray(res?.items) ? res.items : []
    totalCount.value = res?.totalCount || 0
  } catch (err) {
    notFound.value = true
    category.value = {}
    items.value = []
    totalCount.value = 0
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

// Re-query on filter/sort/page changes: search endpoint when filtered, else the
// curated category endpoint.
async function runQuery () {
  if (!selectedOptionIds.value.length) {
    await loadCategory()
    return
  }
  loading.value = true
  try {
    const res = await storefrontApi.search({
      categoryId: categoryId.value,
      specificationOptionIds: selectedOptionIds.value,
      sort: sort.value || undefined,
      page: page.value,
      pageSize: PAGE_SIZE
    })
    items.value = Array.isArray(res?.items) ? res.items : []
    totalCount.value = res?.totalCount || 0
    searchFacets.value = normalizeFacets(res?.facets)
  } catch (err) {
    items.value = []
    totalCount.value = 0
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onFiltersChange (ids) {
  selectedOptionIds.value = ids
  page.value = 1
  runQuery()
}

function onSortChange () {
  page.value = 1
  runQuery()
}

function onPageChange () {
  runQuery()
  if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' })
}

// Reset all state when navigating between categories.
watch(
  () => route.params.idOrSlug,
  () => {
    page.value = 1
    sort.value = ''
    selectedOptionIds.value = []
    searchFacets.value = []
    loadCategory()
  }
)

onMounted(loadCategory)
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1400px;
  margin: 0 auto;
}
</style>
