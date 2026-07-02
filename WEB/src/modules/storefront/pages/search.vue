<template>
  <q-page class="q-pa-md storefront-container">
    <div class="q-mb-md">
      <div class="text-h5 text-weight-bold">
        <template v-if="query">Results for &ldquo;{{ query }}&rdquo;</template>
        <template v-else>All products</template>
      </div>
    </div>

    <div class="row q-col-gutter-lg">
      <!-- Filters -->
      <div class="col-12 col-md-3">
        <q-card flat bordered class="q-pa-md">
          <FilterPanel
            :facets="facetGroups"
            :model-value="selectedOptionIds"
            show-price
            :min-price="minPrice"
            :max-price="maxPrice"
            @update:model-value="onFiltersChange"
            @update:min-price="(v) => onPriceChange('min', v)"
            @update:max-price="(v) => onPriceChange('max', v)"
          />
        </q-card>
      </div>

      <!-- Results -->
      <div class="col-12 col-md-9">
        <div class="row items-center q-mb-md">
          <div class="text-body2 text-grey-7 col">
            <span class="text-weight-medium text-dark">{{ totalCount }}</span>
            {{ totalCount === 1 ? 'result' : 'results' }}
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

        <!-- No results -->
        <div v-if="!loading && !items.length" class="column flex-center q-py-xl text-center text-grey-6">
          <q-icon name="o_search_off" size="64px" class="q-mb-md" />
          <div class="text-h6 text-grey-8">No products found</div>
          <div class="q-mt-xs">
            We couldn't find anything matching your search. Try different keywords or clear your filters.
          </div>
          <q-btn
            v-if="hasFilters"
            flat
            no-caps
            color="primary"
            label="Clear filters"
            class="q-mt-md"
            @click="clearFilters"
          />
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
  </q-page>
</template>

<script setup>
/*
 * Storefront search results (WO-19, AC-STF-002). Reads the `?q=` query, calls the
 * faceted search endpoint and renders a results grid with a live match count, a
 * facet FilterPanel (spec attributes + price range), sort and pagination. Shows a
 * friendly no-results state; the returned facets are surfaced for refinement.
 */
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { storefrontApi, normalizeFacets, searchSortOptions } from 'modules/storefront/api'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'
import ProductCard from 'modules/storefront/components/ProductCard.vue'
import FilterPanel from 'modules/storefront/components/FilterPanel.vue'

const route = useRoute()
const notify = useNotify()

const PAGE_SIZE = 12
const sortOptions = searchSortOptions

const query = ref('')
const items = ref([])
const totalCount = ref(0)
const page = ref(1)
const sort = ref('relevance')
const selectedOptionIds = ref([])
const minPrice = ref(null)
const maxPrice = ref(null)
const facetGroups = ref([])
const loading = ref(false)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)))

const hasFilters = computed(
  () => selectedOptionIds.value.length > 0 || minPrice.value != null || maxPrice.value != null
)

async function runSearch () {
  loading.value = true
  try {
    const res = await storefrontApi.search({
      query: query.value || undefined,
      specificationOptionIds: selectedOptionIds.value,
      minPrice: minPrice.value ?? undefined,
      maxPrice: maxPrice.value ?? undefined,
      sort: sort.value || undefined,
      page: page.value,
      pageSize: PAGE_SIZE
    })
    items.value = Array.isArray(res?.items) ? res.items : []
    totalCount.value = res?.totalCount || 0
    facetGroups.value = normalizeFacets(res?.facets)
  } catch (err) {
    items.value = []
    totalCount.value = 0
    facetGroups.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onFiltersChange (ids) {
  selectedOptionIds.value = ids
  page.value = 1
  runSearch()
}

function onPriceChange (which, value) {
  if (which === 'min') minPrice.value = value
  else maxPrice.value = value
  page.value = 1
  runSearch()
}

function onSortChange () {
  page.value = 1
  runSearch()
}

function onPageChange () {
  runSearch()
  if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' })
}

function clearFilters () {
  selectedOptionIds.value = []
  minPrice.value = null
  maxPrice.value = null
  page.value = 1
  runSearch()
}

// React to a new query coming from the header search box.
watch(
  () => route.query.q,
  (q) => {
    query.value = typeof q === 'string' ? q : ''
    selectedOptionIds.value = []
    minPrice.value = null
    maxPrice.value = null
    page.value = 1
    runSearch()
  }
)

onMounted(() => {
  query.value = typeof route.query.q === 'string' ? route.query.q : ''
  runSearch()
})
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1400px;
  margin: 0 auto;
}
</style>
