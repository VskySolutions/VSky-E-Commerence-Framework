<template>
  <div v-if="cards.length" class="sf-section">
    <div class="sf-container">
      <div class="sf-section__head"><h2 class="sf-section__title">Shop by Category</h2></div>
      <div class="row q-col-gutter-md">
        <div v-for="cat in cards" :key="cat.id" class="col-6 col-md-3">
          <router-link :to="categoryTo(cat)" class="sf-cat-card">
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
  background: var(--sf-surface-alt);
  border: 1px solid var(--sf-border);
  transition: transform var(--sf-transition), box-shadow var(--sf-transition), border-color var(--sf-transition);
}
.sf-cat-card:hover { transform: translateY(-4px); box-shadow: var(--sf-shadow-hover); border-color: var(--sf-accent); }
.sf-cat-card__overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  text-align: center;
  padding: 18px;
  color: var(--sf-heading);
}
.sf-cat-card__name { font-size: 18px; font-weight: 700; }
.sf-cat-card__count { font-size: 12px; color: var(--sf-muted); margin-top: 2px; }
.sf-cat-card__link {
  margin-top: 10px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--sf-accent);
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
</style>
