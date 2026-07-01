<template>
  <div class="aside-header row items-center no-wrap q-px-md q-py-md">
    <q-avatar
      v-if="tenant.logoUrl"
      size="36px"
      class="q-mr-sm"
      square
    >
      <img :src="tenant.logoUrl" alt="logo" />
    </q-avatar>
    <q-icon
      v-else
      name="o_storefront"
      size="30px"
      color="primary"
      class="q-mr-sm"
    />

    <div class="column">
      <div class="text-subtitle1 text-weight-medium ellipsis">
        {{ tenant.brandName }}
      </div>
      <div v-if="tenant.activeTenant" class="text-caption text-muted ellipsis">
        {{ tenant.activeTenant.tenantName }}
      </div>
    </div>
  </div>
</template>

<script setup>
import { useTenantStore } from 'stores/tenant'

const tenant = useTenantStore()
</script>

<style scoped>
.aside-header {
  min-height: 64px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}
</style>
