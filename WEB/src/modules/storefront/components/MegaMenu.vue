<template>
  <nav class="sf-megamenu">
    <div class="sf-container row items-center no-wrap">
      <!-- Mobile: hamburger opens the category drawer -->
      <q-btn
        flat
        dense
        round
        icon="o_menu"
        class="lt-md"
        aria-label="Browse categories"
        @click="drawer = true"
      />
      <span class="lt-md text-weight-medium q-ml-sm">Shop by category</span>

      <!-- Desktop: horizontal top-level links with hover dropdown -->
      <div class="gt-sm row items-center no-wrap sf-megamenu__bar">
        <router-link class="sf-megamenu__link" :class="{ 'sf-megamenu__link--active': isHome }" :to="{ name: 'shop-home' }">
          Home
        </router-link>

        <div v-for="cat in topLevel" :key="cat.id" class="sf-megamenu__item">
          <router-link
            class="sf-megamenu__link"
            :class="{ 'sf-megamenu__link--active': activeId === cat.id }"
            :to="categoryTo(cat)"
          >
            {{ cat.name }}
            <q-icon v-if="cat.children && cat.children.length" name="o_expand_more" size="18px" class="q-ml-xs" />
          </router-link>

          <q-menu
            v-if="cat.children && cat.children.length"
            anchor="bottom left"
            self="top left"
            :offset="[0, 0]"
            class="sf-megamenu__dropdown q-pa-lg"
          >
            <div class="row q-col-gutter-xl no-wrap" style="min-width: 420px">
              <div
                v-for="col in chunkedChildren(cat.children)"
                :key="col.key"
                class="col"
              >
                <div v-for="child in col.items" :key="child.id" class="q-mb-md">
                  <router-link class="sf-megamenu__col-title block" :to="categoryTo(child)" v-close-popup>
                    {{ child.name }}
                  </router-link>
                  <router-link
                    v-for="g in (child.children || []).slice(0, 6)"
                    :key="g.id"
                    class="sf-megamenu__sublink"
                    :to="categoryTo(g)"
                    v-close-popup
                  >
                    {{ g.name }}
                  </router-link>
                </div>
              </div>
            </div>
          </q-menu>
        </div>
      </div>
    </div>

    <!-- Mobile drawer: accordion category tree -->
    <q-drawer v-model="drawer" side="left" overlay bordered :width="290" class="sf-mobile-nav">
      <div class="row items-center justify-between q-pa-md sf-mobile-nav__head">
        <span class="text-weight-bold">Categories</span>
        <q-btn flat dense round icon="o_close" color="grey-8" @click="drawer = false" />
      </div>
      <q-scroll-area style="height: calc(100% - 60px)">
        <q-list>
          <q-item clickable :to="{ name: 'shop-home' }" @click="drawer = false">
            <q-item-section avatar><q-icon name="o_home" /></q-item-section>
            <q-item-section>Home</q-item-section>
          </q-item>
          <template v-for="cat in topLevel" :key="cat.id">
            <q-expansion-item
              v-if="cat.children && cat.children.length"
              :label="cat.name"
              dense
              expand-separator
            >
              <q-item
                clickable
                :to="categoryTo(cat)"
                class="q-pl-lg"
                @click="drawer = false"
              >
                <q-item-section>All {{ cat.name }}</q-item-section>
              </q-item>
              <q-item
                v-for="child in cat.children"
                :key="child.id"
                clickable
                :to="categoryTo(child)"
                class="q-pl-lg"
                @click="drawer = false"
              >
                <q-item-section>{{ child.name }}</q-item-section>
                <q-item-section side>{{ child.productCount }}</q-item-section>
              </q-item>
            </q-expansion-item>
            <q-item v-else clickable :to="categoryTo(cat)" @click="drawer = false">
              <q-item-section>{{ cat.name }}</q-item-section>
              <q-item-section side>{{ cat.productCount }}</q-item-section>
            </q-item>
          </template>
        </q-list>
      </q-scroll-area>
    </q-drawer>
  </nav>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useCategories } from 'modules/storefront/composables/useCategories'

const route = useRoute()
const { categories, loadCategories } = useCategories()

const drawer = ref(false)

// Show a reasonable number of top-level links; the rest live in the mobile drawer.
const topLevel = computed(() => categories.value.slice(0, 8))
const isHome = computed(() => route.name === 'shop-home')
const activeId = computed(() => (route.name === 'shop-category' ? route.params.idOrSlug : null))

function categoryTo (cat) {
  return { name: 'shop-category', params: { idOrSlug: cat.slug || cat.id } }
}

// Split a category's children into up to 3 columns for the dropdown.
function chunkedChildren (children) {
  const cols = Math.min(3, Math.max(1, Math.ceil(children.length / 5)))
  const perCol = Math.ceil(children.length / cols)
  const out = []
  for (let i = 0; i < cols; i++) {
    out.push({ key: i, items: children.slice(i * perCol, (i + 1) * perCol) })
  }
  return out
}

onMounted(loadCategories)
</script>

<style scoped lang="scss">
.sf-megamenu__bar { flex: 1; overflow: hidden; }
.sf-megamenu__item { display: inline-flex; }
.sf-mobile-nav__head { border-bottom: 1px solid var(--sf-border); }
</style>
