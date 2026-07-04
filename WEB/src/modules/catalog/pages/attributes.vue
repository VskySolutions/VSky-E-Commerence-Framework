<template>
  <q-page class="app-page">
    <AppListHeader
      title="Attributes"
      subtitle="Manage the global attribute library used for variants and storefront filters."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Catalog' }, { label: 'Attributes' }]"
      :show-add="false"
    />

    <q-tabs
      v-model="tab"
      dense
      no-caps
      align="left"
      active-color="primary"
      indicator-color="primary"
      class="text-grey-7 q-mb-md app-attributes-tabs"
      @update:model-value="onTab"
    >
      <q-tab name="product" label="Product Attributes" icon="o_palette" />
      <q-tab name="specification" label="Specification Attributes" icon="o_tune" />
    </q-tabs>

    <q-tab-panels v-model="tab" animated class="bg-transparent">
      <q-tab-panel name="product" class="q-pa-none">
        <ProductAttributesPanel />
      </q-tab-panel>
      <q-tab-panel name="specification" class="q-pa-none">
        <SpecificationAttributesPanel />
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup>
/*
 * Catalog → Attributes (WO-15): the global attribute library admin screen, with two
 * sub-tabs — Product Attributes (variant-driving, Dropdown/Button/Swatch) and
 * Specification Attributes (filterable facets). Deep-linkable via ?tab=.
 */
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppListHeader from 'components/common/AppListHeader.vue'
import ProductAttributesPanel from 'modules/catalog/components/ProductAttributesPanel.vue'
import SpecificationAttributesPanel from 'modules/catalog/components/SpecificationAttributesPanel.vue'

const route = useRoute()
const router = useRouter()

const tab = ref(route.query.tab === 'specification' ? 'specification' : 'product')

function onTab (value) {
  router.replace({ query: { ...route.query, tab: value } })
}
</script>
