<template>
  <q-layout view="hHh lpR fFf">
    <q-header elevated class="bg-white text-dark">
      <q-toolbar class="storefront-toolbar q-py-sm">
        <q-btn
          flat
          no-caps
          :to="{ name: 'shop-home' }"
          class="text-weight-bold q-px-sm"
          aria-label="Home"
        >
          <q-icon name="o_storefront" size="24px" color="primary" :class="$q.screen.gt.xs ? 'q-mr-sm' : ''" />
          <span class="gt-xs">VSky Shop</span>
        </q-btn>

        <!-- Prominent search with autocomplete -->
        <q-input
          v-model="searchText"
          dense
          rounded
          outlined
          bg-color="grey-1"
          placeholder="Search products"
          class="storefront-search col q-mx-sm"
          debounce="300"
          @update:model-value="onQueryChange"
          @keyup.enter="onEnter"
        >
          <template #prepend>
            <q-icon name="o_search" class="cursor-pointer" @click="onEnter" />
          </template>
          <template #append>
            <q-icon v-if="searchText" name="o_close" class="cursor-pointer" @click="clearSearch" />
          </template>

          <q-menu
            v-model="suggestOpen"
            fit
            no-focus
            no-refocus
            no-parent-event
            anchor="bottom left"
            self="top left"
          >
            <q-list style="min-width: 240px">
              <template v-if="suggestions.products.length">
                <q-item-label header class="q-py-xs">Products</q-item-label>
                <q-item
                  v-for="(name, i) in suggestions.products"
                  :key="'p-' + i"
                  v-close-popup
                  clickable
                  dense
                  @click="selectSuggestion(name)"
                >
                  <q-item-section avatar>
                    <q-icon name="o_inventory_2" size="18px" color="grey-6" />
                  </q-item-section>
                  <q-item-section>{{ name }}</q-item-section>
                </q-item>
              </template>

              <template v-if="suggestions.categories.length">
                <q-item-label header class="q-py-xs">Categories</q-item-label>
                <q-item
                  v-for="(name, i) in suggestions.categories"
                  :key="'c-' + i"
                  v-close-popup
                  clickable
                  dense
                  @click="selectSuggestion(name)"
                >
                  <q-item-section avatar>
                    <q-icon name="o_category" size="18px" color="grey-6" />
                  </q-item-section>
                  <q-item-section>{{ name }}</q-item-section>
                </q-item>
              </template>
            </q-list>
          </q-menu>
        </q-input>

        <LanguageSelector class="q-mr-xs" />

        <q-btn
          flat
          no-caps
          dense
          icon="o_compare"
          :label="$q.screen.gt.sm ? 'Compare' : ''"
          :to="{ name: 'shop-compare' }"
          aria-label="Compare"
        >
          <q-badge v-if="compareIds.length" color="red" floating>{{ compareIds.length }}</q-badge>
          <q-tooltip>Compare products</q-tooltip>
        </q-btn>
      </q-toolbar>
    </q-header>

    <q-page-container>
      <router-view />
    </q-page-container>

    <q-footer class="bg-grey-9 text-grey-4">
      <div class="storefront-footer q-pa-md row items-center justify-between">
        <div class="row items-center">
          <q-icon name="o_storefront" size="20px" class="q-mr-sm" />
          <span>VSky Shop &copy; {{ year }}</span>
        </div>
        <div class="row q-gutter-md">
          <q-btn flat dense no-caps size="sm" label="Home" :to="{ name: 'shop-home' }" />
          <q-btn flat dense no-caps size="sm" label="Compare" :to="{ name: 'shop-compare' }" />
        </div>
      </div>
    </q-footer>
  </q-layout>
</template>

<script setup>
/*
 * Public storefront shell (WO-19). Chrome-light layout for anonymous shoppers:
 * a brand/home link, a prominent debounced search with autocomplete suggestions
 * (products + categories) that navigate to the search results route, a language
 * selector and a compare link with a live count badge. No admin drawer, no auth
 * requirement, no authenticated-user assumptions.
 */
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import { storefrontApi } from 'modules/storefront/api'
import { useCompare } from 'modules/storefront/composables/useStorefrontStorage'
import LanguageSelector from 'modules/storefront/components/LanguageSelector.vue'

const router = useRouter()
const $q = useQuasar()
const { compareIds } = useCompare()

const year = new Date().getFullYear()

const searchText = ref('')
const suggestions = ref({ products: [], categories: [] })
const suggestOpen = ref(false)

async function onQueryChange (val) {
  const q = (val || '').trim()
  if (q.length < 2) {
    suggestOpen.value = false
    suggestions.value = { products: [], categories: [] }
    return
  }
  try {
    const result = await storefrontApi.autocomplete(q)
    const products = Array.isArray(result?.products) ? result.products : []
    const categories = Array.isArray(result?.categories) ? result.categories : []
    suggestions.value = { products, categories }
    suggestOpen.value = products.length > 0 || categories.length > 0
  } catch (e) {
    suggestOpen.value = false
    suggestions.value = { products: [], categories: [] }
  }
}

function goSearch (q) {
  suggestOpen.value = false
  const term = (q || '').trim()
  if (!term) return
  router.push({ name: 'shop-search', query: { q: term } })
}

function onEnter (evt) {
  // Prefer the live DOM value in case the debounced v-model has not caught up.
  const value = evt && evt.target ? evt.target.value : searchText.value
  goSearch(value)
}

function selectSuggestion (name) {
  searchText.value = name
  goSearch(name)
}

function clearSearch () {
  searchText.value = ''
  suggestOpen.value = false
  suggestions.value = { products: [], categories: [] }
}
</script>

<style scoped lang="scss">
.storefront-toolbar {
  gap: 4px;
}

.storefront-search {
  max-width: 640px;
}

.storefront-footer {
  max-width: 1400px;
  margin: 0 auto;
}
</style>
