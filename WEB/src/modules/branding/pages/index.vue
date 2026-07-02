<template>
  <q-page class="app-page">
    <AppListHeader
      title="Branding"
      subtitle="Tenant colour palette, logo, typography and contact details."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Branding' }]"
    >
      <template #actions>
        <q-btn flat color="primary" icon="o_refresh" label="Reload" no-caps :loading="loading" @click="load" />
        <q-btn v-if="canWrite" unelevated color="primary" icon="o_edit" label="Edit branding" no-caps @click="drawerOpen = true" />
      </template>
    </AppListHeader>

    <div class="row q-col-gutter-md">
      <div class="col-12 col-md-4">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Identity</div>
            <div class="text-body2">Brand: <strong>{{ branding.brandName || '—' }}</strong></div>
            <div class="text-body2">Domain: {{ branding.domain || '—' }}</div>
            <div class="text-body2">Font: {{ branding.fontFamily || 'default (Poppins/Roboto)' }}</div>
            <div class="text-body2">Language: {{ branding.defaultLanguage || '—' }}</div>
          </q-card-section>
        </q-card>
      </div>

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
            <q-img v-if="branding.logoUrl" :src="branding.logoUrl" style="max-height: 96px" fit="contain" />
            <div v-else class="text-grey-7">No logo configured.</div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Contact</div>
            <div class="text-body2">Email: {{ branding.supportEmail || '—' }}</div>
            <div class="text-body2">Phone: {{ branding.supportPhone || '—' }}</div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-6">
        <q-card flat bordered>
          <q-card-section>
            <div class="text-subtitle1 q-mb-sm">Social links</div>
            <template v-if="socialEntries.length">
              <div v-for="s in socialEntries" :key="s.key" class="text-body2 ellipsis">
                <span class="text-capitalize text-grey-8">{{ s.key }}:</span>
                <a :href="s.value" target="_blank" rel="noopener" class="q-ml-xs">{{ s.value }}</a>
              </div>
            </template>
            <div v-else class="text-grey-7">No social links configured.</div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <q-banner v-if="!canWrite" class="bg-grey-2 rounded-borders q-mt-md text-grey-8">
      You have read-only access to branding.
    </q-banner>

    <BrandingFormDrawer
      v-model="drawerOpen"
      :item="branding"
      :saving="saving"
      @submit="onSubmit"
      @cancel="drawerOpen = false"
    />
  </q-page>
</template>

<script setup>
/*
 * Branding page (WO-9 REQ-TEN-001): read-only summary cards plus an "Edit
 * branding" drawer that persists via PUT /api/tenant/branding and then refreshes
 * the global tenant branding (CSS custom properties + shell brand name/logo).
 */
import { ref, computed, onMounted } from 'vue'
import { brandingApi } from 'modules/branding/api'
import { getApiErrorMessage } from 'services/api'
import { useTenantStore } from 'stores/tenant'
import { useNotify } from 'composables/useNotify'
import { usePermissions, Permissions } from 'composables/usePermissions'
import BrandingFormDrawer from 'modules/branding/components/BrandingFormDrawer.vue'

const tenant = useTenantStore()
const notify = useNotify()
const { has } = usePermissions()

const canWrite = computed(() => has(Permissions.BrandingWrite))

const branding = ref({})
const loading = ref(false)
const saving = ref(false)
const drawerOpen = ref(false)

const swatches = computed(() => [
  { key: 'primary', label: 'Primary', value: branding.value.primaryColor },
  { key: 'secondary', label: 'Secondary', value: branding.value.secondaryColor },
  { key: 'accent', label: 'Accent', value: branding.value.accentColor }
])

const socialEntries = computed(() => {
  try {
    const obj = branding.value.socialLinksJson ? JSON.parse(branding.value.socialLinksJson) : null
    if (!obj || typeof obj !== 'object') return []
    return Object.entries(obj)
      .filter(([, v]) => !!v)
      .map(([key, value]) => ({ key, value }))
  } catch (e) {
    return []
  }
})

async function load () {
  loading.value = true
  try {
    branding.value = (await brandingApi.get()) || {}
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

async function onSubmit (payload) {
  saving.value = true
  try {
    branding.value = await brandingApi.update(payload)
    notify.success('Branding updated')
    drawerOpen.value = false
    // Refresh the shell's applied branding (colours, brand name, logo).
    await tenant.loadBranding().catch(() => {})
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.swatch {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(0, 0, 0, 0.1);
}
</style>
