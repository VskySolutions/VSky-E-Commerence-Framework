<template>
  <q-layout view="lHh Lpr lFf">
    <q-header elevated>
      <q-toolbar>
        <q-btn
          flat
          dense
          round
          icon="o_menu"
          aria-label="Menu"
          @click="toggleDrawer"
        />

        <q-btn flat no-caps class="text-weight-medium q-ml-xs" @click="goHome">
          <q-icon name="o_storefront" size="22px" class="q-mr-sm" />
          {{ tenant.brandName }}
        </q-btn>

        <q-space />

        <q-btn-dropdown
          v-if="tenant.hasMultipleTenants"
          flat
          no-caps
          class="tenant-switcher q-mr-sm"
          icon="o_business"
          :label="activeTenantLabel"
        >
          <q-list style="min-width: 220px">
            <q-item-label header class="text-caption">Switch tenant</q-item-label>
            <q-item
              v-for="assignment in tenant.assignments"
              :key="assignment.tenantId"
              v-close-popup
              clickable
              @click="onSwitchTenant(assignment.tenantId)"
            >
              <q-item-section>{{ assignment.tenantName }}</q-item-section>
              <q-item-section
                v-if="String(assignment.tenantId) === String(tenant.activeTenantId)"
                side
              >
                <q-icon name="o_check" color="primary" />
              </q-item-section>
            </q-item>
          </q-list>
        </q-btn-dropdown>

        <UserInfo />
      </q-toolbar>
    </q-header>

    <q-drawer
      v-model="leftDrawerOpen"
      show-if-above
      bordered
      :width="292"
      :breakpoint="900"
    >
      <div class="column full-height no-wrap">
        <AsideHeader />
        <q-scroll-area class="col">
          <AppMenu />
        </q-scroll-area>
      </div>
    </q-drawer>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup>
/*
 * Authenticated application shell (WO-94 Step 9). Header with menu toggle,
 * brand button, tenant switcher (when the user has multiple tenants) and the
 * user menu. A 292px left drawer (persisted to LocalStorage["leftDrawerOpen"])
 * hosts the aside header + navigation menu.
 */
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTenantStore } from 'stores/tenant'
import { STORAGE_KEYS, getItem, setItem } from 'services/storage'
import { useNotify } from 'composables/useNotify'
import AppMenu from 'components/app_menu.vue'
import AsideHeader from 'shared/aside_header.vue'
import UserInfo from 'shared/user_info.vue'

const tenant = useTenantStore()
const router = useRouter()
const notify = useNotify()

const leftDrawerOpen = ref(getItem(STORAGE_KEYS.LEFT_DRAWER_OPEN, true) !== false)
watch(leftDrawerOpen, (val) => setItem(STORAGE_KEYS.LEFT_DRAWER_OPEN, val))

function toggleDrawer () {
  leftDrawerOpen.value = !leftDrawerOpen.value
}

function goHome () {
  router.push('/dashboard')
}

const activeTenantLabel = computed(() => tenant.activeTenant?.tenantName || 'Tenant')

async function onSwitchTenant (tenantId) {
  await tenant.switchTenant(tenantId)
  // Broadcast so open pages can reload tenant-scoped data.
  window.dispatchEvent(new CustomEvent('tenant-switched', { detail: { tenantId } }))
  notify.info('Tenant switched')
}

onMounted(() => {
  tenant.loadBranding().catch(() => {})
})
</script>
