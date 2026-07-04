<template>
  <q-page class="app-page">
    <AppListHeader
      title="Stores"
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Stores' }]"
      :show-add="canWrite"
      add-label="New store"
      @add="onAdd"
    />

    <AppDataTable
      page-key="stores"
      row-key="id"
      title="All stores"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
      @refresh="reload"
    >
      <template #body-cell-name="cell">
        <q-td :props="cell"><a class="text-primary cursor-pointer text-weight-medium" @click="onEdit(cell.row)">{{ cell.row.name }}</a></q-td>
      </template>
      <template #body-cell-location="cell">
        <q-td :props="cell">{{ [cell.row.city, cell.row.countryCode].filter(Boolean).join(', ') || '—' }}</q-td>
      </template>
      <template #body-cell-status="cell">
        <q-td :props="cell"><q-badge :color="storeStatus(cell.row).color" :label="storeStatus(cell.row).label" /></q-td>
      </template>
      <template #body-cell-guestOrderingEnabled="cell">
        <q-td :props="cell"><q-icon :name="cell.row.guestOrderingEnabled ? 'o_check_circle' : 'o_block'" :color="cell.row.guestOrderingEnabled ? 'positive' : 'grey'" /></q-td>
      </template>
      <template #actions="{ row }">
        <q-btn flat round dense icon="o_map" @click="openZones(row)"><q-tooltip>Delivery zones</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)"><q-tooltip>Edit</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)"><q-tooltip>Delete</q-tooltip></q-btn>
      </template>
    </AppDataTable>

    <StoreFormDrawer v-model="drawerOpen" :item="editing" :saving="saving" @submit="onSubmit" @cancel="drawerOpen = false" />
    <DeliveryZonesDialog v-model="zonesOpen" :store="zonesStore" />
  </q-page>
</template>

<script setup>
/* Admin store configuration (WO-113): store CRUD (full profile + toggles) + per-store delivery zones. */
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { storeApi, storeStatus } from 'modules/stores/api'
import StoreFormDrawer from 'modules/stores/components/StoreFormDrawer.vue'
import DeliveryZonesDialog from 'modules/stores/components/DeliveryZonesDialog.vue'

const { has } = usePermissions()
const notify = useNotify()
const canWrite = computed(() => has(Permissions.StoresWrite))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'location', label: 'Location', field: 'city', align: 'left' },
  { name: 'guestOrderingEnabled', label: 'Guest', field: 'guestOrderingEnabled', align: 'center' },
  { name: 'status', label: 'Status', field: 'status', align: 'center' }
]

const rows = ref([])
const loading = ref(false)
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)
const zonesOpen = ref(false)
const zonesStore = ref(null)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await storeApi.list({ page: p.page, pageSize: p.rowsPerPage })
    const items = Array.isArray(result) ? result : result?.items || []
    rows.value = items
    pagination.value = { ...p, rowsNumber: Array.isArray(result) ? items.length : result?.totalCount ?? items.length }
  } catch (err) {
    rows.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onRequest (props) { fetch(props) }
function reload () { fetch({ pagination: { ...pagination.value, page: 1 } }) }

function onAdd () { editing.value = null; drawerOpen.value = true }
async function onEdit (row) {
  try { editing.value = await storeApi.get(row.id) } catch (e) { editing.value = { ...row } }
  drawerOpen.value = true
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await storeApi.update(editing.value.id, payload)
      notify.success('Store updated')
    } else {
      await storeApi.create(payload)
      notify.success('Store created')
    }
    drawerOpen.value = false
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

async function onDelete (row) {
  if (!(await deleteConfirmation(`the store "${row.name}"`))) return
  try {
    await storeApi.remove(row.id)
    notify.success('Store deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

function openZones (row) { zonesStore.value = row; zonesOpen.value = true }

onMounted(() => fetch())
</script>
