<template>
  <q-page class="storefront-root">
    <!-- Section order is fixed here. A dynamic Home Page Sections API (WO-96/WO-100)
         would drive ordering / enable-disable / per-section config — flagged on WO-110. -->
    <HeroCarousel />
    <FeatureBar />
    <CategoryGrid :limit="8" />
    <ProductTabs heading="Featured Products" />
    <DoubleBanner />

    <!-- Recently viewed (client-side history) -->
    <div v-if="showRecent" class="sf-section">
      <div class="sf-container">
        <RecentlyViewed />
      </div>
    </div>

    <NewsletterStrip />
  </q-page>
</template>

<script setup>
/*
 * Porto-inspired storefront home page (WO-110). Renders the section stack: hero
 * carousel, feature bar, featured categories, a tabbed product row, a double
 * promotional banner, recently-viewed, and a newsletter strip.
 *
 * The dynamic Home Page Sections API (WO-96/WO-100) does not exist yet, so the
 * section order is static here and hero/banner content is derived (categories +
 * branded gradients) rather than banner-driven — all flagged on WO-110. Each
 * section component degrades gracefully when its data source is empty.
 */
import { computed } from 'vue'
import { useRecentlyViewed } from 'modules/storefront/composables/useStorefrontStorage'
import HeroCarousel from 'modules/storefront/components/home/HeroCarousel.vue'
import FeatureBar from 'modules/storefront/components/home/FeatureBar.vue'
import CategoryGrid from 'modules/storefront/components/home/CategoryGrid.vue'
import ProductTabs from 'modules/storefront/components/home/ProductTabs.vue'
import DoubleBanner from 'modules/storefront/components/home/DoubleBanner.vue'
import NewsletterStrip from 'modules/storefront/components/home/NewsletterStrip.vue'
import RecentlyViewed from 'modules/storefront/components/RecentlyViewed.vue'

const { recentlyViewedIds } = useRecentlyViewed()
const showRecent = computed(() => (recentlyViewedIds.value || []).length > 0)
</script>
