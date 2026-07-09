<template>
  <q-page class="app-page">
    <AppListHeader title="Shipping" :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Shipping' }]" :show-add="false" show-back @back="router.push('/dashboard')" />
    <div class="row items-center q-col-gutter-md q-mb-md">
      <div class="col-12 col-md-6">
        <q-tabs v-model="tab" dense no-caps align="left" active-color="primary" indicator-color="primary" class="text-grey-7" @update:model-value="onTab">
          <q-tab name="methods" label="Methods" icon="o_local_shipping" />
          <q-tab name="zones" label="Zones" icon="o_map" />
        </q-tabs>
      </div>
      <div class="col-12 col-md-6">
        <div class="row items-center justify-end q-gutter-sm">
          <q-input v-model="search" dense outlined debounce="400" :placeholder="searchPlaceholder" style="min-width: 220px">
            <template #prepend><q-icon name="o_search" /></template>
            <template v-if="search" #append><q-icon name="o_close" class="cursor-pointer" @click="search = ''" /></template>
          </q-input>
          <q-btn outline color="primary" no-caps icon="o_tune" label="Advanced" @click="panelRef?.openFilters()">
            <q-badge v-if="filterCount" color="red" floating>{{ filterCount }}</q-badge>
          </q-btn>
          <q-btn v-if="canWrite" color="primary" unelevated no-caps icon="o_add" :label="addLabel" @click="panelRef?.onAdd()" />
        </div>
      </div>
    </div>

    <component :is="activeComponent" ref="panelRef" :search="search" @filter-count="filterCount = $event" />
  </q-page>
</template>

<script setup>
/* Shipping admin (WO-116): custom methods + zones sub-tabs, with a shared toolbar next to the tabs
 * driving the active panel (dynamic component). */
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { usePermissions } from 'composables/usePermissions'
import AppListHeader from 'components/common/AppListHeader.vue'
import ShippingMethodsPanel from 'modules/shipping/components/ShippingMethodsPanel.vue'
import ShippingZonesPanel from 'modules/shipping/components/ShippingZonesPanel.vue'

const route = useRoute()
const router = useRouter()
const { has } = usePermissions()
const canWrite = computed(() => has('Stores.Write'))

const tab = ref(route.query.tab === 'zones' ? 'zones' : 'methods')
const search = ref('')
const filterCount = ref(0)
const panelRef = ref(null)

const isZones = computed(() => tab.value === 'zones')
const activeComponent = computed(() => (isZones.value ? ShippingZonesPanel : ShippingMethodsPanel))
const searchPlaceholder = computed(() => (isZones.value ? 'Search zones' : 'Search methods'))
const addLabel = computed(() => (isZones.value ? 'New zone' : 'New method'))

function onTab (value) {
  search.value = ''
  filterCount.value = 0
  router.replace({ query: { ...route.query, tab: value } })
}
</script>
