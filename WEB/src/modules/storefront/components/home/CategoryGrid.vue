<template>
  <div v-if="cards.length" class="sf-section">
    <div class="sf-container">
      <div class="sf-section__head"><h2 class="sf-section__title">Shop by Category</h2></div>
      <div class="row q-col-gutter-md">
        <div v-for="(cat, i) in cards" :key="cat.id" class="col-6 col-md-3">
          <router-link :to="categoryTo(cat)" class="sf-cat-card" :style="{ background: gradientFor(i) }">
            <div class="sf-cat-card__overlay">
              <div class="sf-cat-card__name">{{ cat.name }}</div>
              <div v-if="cat.productCount" class="sf-cat-card__count">{{ cat.productCount }} products</div>
              <span class="sf-cat-card__link">Shop Now <q-icon name="o_arrow_forward" size="14px" /></span>
            </div>
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
/*
 * Porto featured-categories grid (WO-110). Sourced from the public category tree.
 * Category images are not in the catalog DTO yet (flagged on WO-111), so cards use
 * branded gradient backgrounds; swap to background images when available.
 */
import { computed, onMounted } from 'vue'
import { useCategories } from 'modules/storefront/composables/useCategories'

const props = defineProps({ limit: { type: Number, default: 8 } })

const { categories, loadCategories } = useCategories()
const cards = computed(() => categories.value.slice(0, props.limit))

const gradients = [
  'linear-gradient(135deg, #1a1a2e, #3a3a6a)',
  'linear-gradient(135deg, #7c3aed, #a855f7)',
  'linear-gradient(135deg, #0891b2, #06b6d4)',
  'linear-gradient(135deg, #e31e24, #f97316)',
  'linear-gradient(135deg, #059669, #10b981)',
  'linear-gradient(135deg, #4338ca, #6366f1)',
  'linear-gradient(135deg, #b45309, #f59e0b)',
  'linear-gradient(135deg, #be185d, #ec4899)'
]
function gradientFor (i) { return gradients[i % gradients.length] }
function categoryTo (cat) { return { name: 'shop-category', params: { idOrSlug: cat.slug || cat.id } } }

onMounted(loadCategories)
</script>

<style scoped lang="scss">
.sf-cat-card {
  display: block;
  position: relative;
  aspect-ratio: 4 / 3;
  border-radius: var(--sf-radius);
  overflow: hidden;
  text-decoration: none;
  transition: transform var(--sf-transition), box-shadow var(--sf-transition);
}
.sf-cat-card:hover { transform: translateY(-4px); box-shadow: var(--sf-shadow-hover); }
.sf-cat-card__overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  padding: 18px;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.45), transparent 60%);
  color: #fff;
}
.sf-cat-card__name { font-size: 18px; font-weight: 700; }
.sf-cat-card__count { font-size: 12px; opacity: 0.85; }
.sf-cat-card__link {
  margin-top: 8px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
</style>
