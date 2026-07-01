<template>
  <q-page class="app-page">
    <AppListHeader
      title="Branding"
      subtitle="Tenant colour palette, logo and typography."
    >
      <template #actions>
        <q-btn flat color="primary" icon="o_refresh" label="Reload" no-caps :loading="loading" @click="reload" />
      </template>
    </AppListHeader>

    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Colours</div>
            <div v-for="c in swatches" :key="c.key" class="row items-center q-mb-sm">
              <div class="swatch q-mr-sm" :style="{ background: c.value || '#e0e0e0' }" />
              <div class="col">
                <div class="text-body2">{{ c.label }}</div>
                <div class="text-caption text-grey-7">{{ c.value || 'not set' }}</div>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Logo</div>
            <q-img v-if="tenant.logoUrl" :src="tenant.logoUrl" style="max-height: 96px" fit="contain" />
            <div v-else class="text-grey-7">No logo configured.</div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Typography</div>
            <div class="text-body2">Brand: <strong>{{ tenant.brandName }}</strong></div>
            <div class="text-body2">Font: {{ tenant.fontFamily || 'default (Poppins/Roboto)' }}</div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <q-banner v-if="canWrite" class="bg-blue-1 text-primary rounded-borders q-mt-md">
      You have branding write access — editing UI to follow.
    </q-banner>
  </q-page>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useTenantStore } from 'stores/tenant'
import { usePermissions, Permissions } from 'composables/usePermissions'

const tenant = useTenantStore()
const { has } = usePermissions()
const loading = ref(false)

const canWrite = computed(() => has(Permissions.BrandingWrite))

const swatches = computed(() => [
  { key: 'primary', label: 'Primary', value: tenant.branding.primaryColor },
  { key: 'secondary', label: 'Secondary', value: tenant.branding.secondaryColor },
  { key: 'accent', label: 'Accent', value: tenant.branding.accentColor }
])

async function reload () {
  loading.value = true
  try {
    await tenant.loadBranding()
  } finally {
    loading.value = false
  }
}

onMounted(reload)
</script>

<style scoped>
.swatch {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(0, 0, 0, 0.1);
}
</style>
