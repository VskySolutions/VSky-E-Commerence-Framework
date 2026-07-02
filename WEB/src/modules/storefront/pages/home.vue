<template>
  <q-page class="q-pa-md storefront-container">
    <section class="storefront-hero rounded-borders q-pa-lg q-mb-lg bg-primary text-white">
      <div class="text-h4 text-weight-bold">Welcome to VSky Shop</div>
      <div class="text-subtitle1 q-mt-sm">Discover our latest products.</div>
    </section>

    <section class="q-mb-xl">
      <div class="text-h6 q-mb-md">New arrivals</div>

      <q-inner-loading :showing="loading" color="primary" />

      <div v-if="!loading && !products.length" class="text-grey-6 q-py-lg text-center">
        No products are available yet.
      </div>

      <div class="row q-col-gutter-md">
        <div
          v-for="p in products"
          :key="p.id"
          class="col-6 col-sm-4 col-md-3 col-lg-2"
        >
          <ProductCard :product="p" />
        </div>
      </div>
    </section>

    <RecentlyViewed />
  </q-page>
</template>

<script setup>
/*
 * Storefront landing page (WO-19). Shows the newest published products (via the
 * search endpoint sorted by "newest") plus the recently-viewed row. Resilient to
 * an empty / unavailable catalog.
 */
import { ref, onMounted } from 'vue'
import { storefrontApi } from 'modules/storefront/api'
import ProductCard from 'modules/storefront/components/ProductCard.vue'
import RecentlyViewed from 'modules/storefront/components/RecentlyViewed.vue'

const products = ref([])
const loading = ref(false)

async function load () {
  loading.value = true
  try {
    const result = await storefrontApi.search({ sort: 'newest', page: 1, pageSize: 12 })
    products.value = Array.isArray(result?.items) ? result.items : []
  } catch (e) {
    products.value = []
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1400px;
  margin: 0 auto;
}
</style>
