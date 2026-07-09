<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="isCreate ? 'New store' : (entity?.name || 'Store')"
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Stores', to: { name: 'stores' } },
        { label: isCreate ? 'New store' : (entity?.name || 'Store') }
      ]"
      :status="!isCreate && entity ? statusInfo.label : ''"
      :status-color="statusInfo.color"
      show-back
      @back="router.push({ name: 'stores' })"
    >
      <template #actions>
        <q-chip
          v-if="saveStatus"
          :icon="saveStatus.icon"
          :color="saveStatus.chip"
          :text-color="saveStatus.text"
          square
          dense
          class="q-mr-sm text-caption"
        >
          <q-spinner v-if="saveStatus.spin" size="14px" class="q-mr-xs" />
          {{ saveStatus.label }}
        </q-chip>
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !isCreate && !entity" class="bg-grey-2 rounded-borders">
      Store not found.
    </q-banner>

    <template v-if="isCreate || entity">
      <div v-if="!isCreate" class="row items-center text-caption text-grey-7 q-mb-sm q-px-xs">
        <q-icon name="o_cloud_sync" size="16px" class="q-mr-xs" />
        Changes are saved automatically as you edit — no need to press save.
      </div>

      <q-card flat bordered class="app-section">
        <q-tabs
          v-model="tab"
          align="left"
          active-color="primary"
          indicator-color="primary"
          class="text-grey-7 app-detail-tabs"
          no-caps
          inline-label
        >
          <q-tab name="general" icon="o_store" label="General" />
          <q-tab name="location" icon="o_place" label="Location" :disable="isCreate" />
          <q-tab name="contact" icon="o_schedule" label="Contact & hours" :disable="isCreate" />
          <q-tab name="zones" icon="o_map" label="Delivery zones" :disable="isCreate" />
        </q-tabs>

        <q-separator />

        <q-tab-panels v-model="tab" animated keep-alive>
          <!-- ============ GENERAL ============ -->
          <q-tab-panel name="general" class="q-gutter-y-sm">
            <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Downtown Store" :disable="!canWrite" />
            <AppTextField v-model="form.orderCapacityLimit" label="Order capacity limit" type="number" placeholder="Max concurrent orders (blank = unlimited)" :disable="!canWrite" />
            <q-separator class="q-my-sm" />
            <div class="column q-gutter-xs">
              <q-toggle v-model="form.isEnabled" label="Enabled" color="primary" :disable="!canWrite" />
              <q-toggle v-model="form.maintenanceMode" label="Maintenance mode" color="orange" :disable="!canWrite" />
              <q-toggle v-model="form.guestOrderingEnabled" label="Guest ordering allowed" color="primary" :disable="!canWrite" />
            </div>
          </q-tab-panel>

          <!-- ============ LOCATION ============ -->
          <q-tab-panel name="location" class="q-gutter-y-sm">
            <AppTextField v-model="form.addressLine1" label="Address line 1" placeholder="Street address" :disable="!canWrite" />
            <AppTextField v-model="form.addressLine2" label="Address line 2" placeholder="Suite, unit (optional)" :disable="!canWrite" />
            <div class="row q-col-gutter-sm">
              <div class="col-6"><AppTextField v-model="form.city" label="City" :disable="!canWrite" /></div>
              <div class="col-6"><AppTextField v-model="form.stateProvince" label="State / Province" :disable="!canWrite" /></div>
            </div>
            <div class="row q-col-gutter-sm">
              <div class="col-6"><AppTextField v-model="form.postalCode" label="Postal code" :disable="!canWrite" /></div>
              <div class="col-6"><AppTextField v-model="form.countryCode" label="Country (ISO-2)" placeholder="US" maxlength="2" :disable="!canWrite" /></div>
            </div>
            <div class="row q-col-gutter-sm">
              <div class="col-6"><AppTextField v-model="form.latitude" label="Latitude" type="number" step="0.000001" placeholder="Used for order routing" :disable="!canWrite" /></div>
              <div class="col-6"><AppTextField v-model="form.longitude" label="Longitude" type="number" step="0.000001" :disable="!canWrite" /></div>
            </div>
          </q-tab-panel>

          <!-- ============ CONTACT & HOURS ============ -->
          <q-tab-panel name="contact" class="q-gutter-y-sm">
            <div class="row q-col-gutter-sm">
              <div class="col-6"><AppTextField v-model="form.contactEmail" label="Contact email" :disable="!canWrite" /></div>
              <div class="col-6"><AppTextField v-model="form.contactPhone" label="Contact phone" :disable="!canWrite" /></div>
            </div>
            <div class="row q-col-gutter-sm">
              <div class="col-6"><AppTextField v-model="form.timeZone" label="Time zone" placeholder="e.g. America/New_York" :disable="!canWrite" /></div>
              <div class="col-6"><AppTextField v-model="form.currencyDisplay" label="Display currency" placeholder="e.g. USD" :disable="!canWrite" /></div>
            </div>

            <q-separator class="q-my-sm" />
            <AppFieldLabel label="Operating hours" />
            <div v-for="row in hours" :key="row.day" class="row items-center q-col-gutter-sm q-mb-xs">
              <div class="col-4 col-md-2 text-body2">{{ row.day }}</div>
              <div class="col-auto"><q-toggle v-model="row.closed" label="Closed" dense :disable="!canWrite" @update:model-value="syncHours" /></div>
              <template v-if="!row.closed">
                <div class="col"><q-input v-model="row.open" dense outlined type="time" :disable="!canWrite" @update:model-value="syncHours" /></div>
                <div class="col"><q-input v-model="row.close" dense outlined type="time" :disable="!canWrite" @update:model-value="syncHours" /></div>
              </template>
            </div>
          </q-tab-panel>

          <!-- ============ DELIVERY ZONES ============ -->
          <q-tab-panel name="zones" class="q-gutter-y-sm">
            <div class="text-body2 text-grey-7 q-mb-sm">Geographic zones this store delivers to, used by the order-routing engine.</div>
            <q-btn v-if="entity" outline color="primary" no-caps icon="o_map" label="Manage delivery zones" @click="zonesOpen = true" />
          </q-tab-panel>
        </q-tab-panels>

        <template v-if="isCreate">
          <q-separator />
          <q-card-actions class="q-pa-md">
            <div class="text-caption text-grey-7">
              Create the store to unlock location, hours and delivery zones — all auto-saved from then on.
            </div>
            <q-space />
            <q-btn v-if="canWrite" unelevated color="primary" no-caps icon="o_check" label="Create store" :loading="creating" @click="create" />
          </q-card-actions>
        </template>
      </q-card>
    </template>

    <DeliveryZonesDialog v-model="zonesOpen" :store="entity" />
  </q-page>
</template>

<script setup>
/*
 * Store create + manage page (full-page auto-save pattern via useDetailForm). Tabs: General / Location /
 * Contact & hours / Delivery zones. Operating hours are edited via a per-day grid that serializes into
 * form.operatingHoursJson so changes auto-save like any field. Delivery zones reuse the existing dialog.
 */
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useDetailForm } from 'composables/useDetailForm'
import { required, maxLength } from 'validators'
import { storeApi, storeStatus, WEEK_DAYS } from 'modules/stores/api'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'
import DeliveryZonesDialog from 'modules/stores/components/DeliveryZonesDialog.vue'

const router = useRouter()
const { has } = usePermissions()
const canWrite = computed(() => has(Permissions.StoresWrite))

const tab = ref('general')
const zonesOpen = ref(false)

// Per-day operating-hours grid, synced into form.operatingHoursJson (a core field) so edits auto-save.
const hours = ref(WEEK_DAYS.map((day) => ({ day, closed: false, open: '09:00', close: '17:00' })))
function parseHours (json) {
  let parsed = {}
  try { parsed = json ? JSON.parse(json) : {} } catch (e) { parsed = {} }
  return WEEK_DAYS.map((day) => {
    const d = parsed[day] || {}
    return { day, closed: d.closed === true, open: d.open || '09:00', close: d.close || '17:00' }
  })
}
function serializeHours () {
  const out = {}
  for (const r of hours.value) out[r.day] = r.closed ? { closed: true } : { open: r.open, close: r.close }
  return JSON.stringify(out)
}
function syncHours () { form.operatingHoursJson = serializeHours() }

function numberOrNull (v) {
  if (v === '' || v === null || v === undefined) return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

function buildPayload (f) {
  return {
    name: (f.name || '').trim(),
    addressLine1: f.addressLine1 || null,
    addressLine2: f.addressLine2 || null,
    city: f.city || null,
    stateProvince: f.stateProvince || null,
    postalCode: f.postalCode || null,
    countryCode: f.countryCode ? String(f.countryCode).toUpperCase() : null,
    latitude: numberOrNull(f.latitude),
    longitude: numberOrNull(f.longitude),
    contactEmail: f.contactEmail || null,
    contactPhone: f.contactPhone || null,
    timeZone: f.timeZone || 'UTC',
    currencyDisplay: f.currencyDisplay || null,
    orderCapacityLimit: numberOrNull(f.orderCapacityLimit),
    operatingHoursJson: f.operatingHoursJson || serializeHours(),
    isEnabled: f.isEnabled,
    maintenanceMode: f.maintenanceMode,
    guestOrderingEnabled: f.guestOrderingEnabled
  }
}

const {
  form, v$, entity, loading, creating, isCreate, saveStatus, create
} = useDetailForm({
  createRouteName: 'store-new',
  detailRouteName: 'store-detail',
  entityLabel: 'store',
  api: storeApi,
  buildPayload,
  empty: {
    name: '', addressLine1: '', addressLine2: '', city: '', stateProvince: '', postalCode: '', countryCode: '',
    latitude: null, longitude: null, contactEmail: '', contactPhone: '', timeZone: 'UTC', currencyDisplay: '',
    orderCapacityLimit: null, operatingHoursJson: '', isEnabled: true, maintenanceMode: false, guestOrderingEnabled: true
  },
  rules: { name: { required, maxLength: maxLength(200) } },
  afterLoad: (e) => { hours.value = parseHours(e.operatingHoursJson) },
  resetExtra: () => { hours.value = parseHours(null) }
})

const statusInfo = computed(() => entity.value ? storeStatus({ isEnabled: form.isEnabled, maintenanceMode: form.maintenanceMode }) : { label: '', color: 'grey' })
</script>

<style scoped lang="scss">
.app-detail-tabs {
  :deep(.q-tab) { min-height: 44px; }
}
</style>
