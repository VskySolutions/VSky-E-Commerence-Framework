<template>
  <div>
    <div class="row justify-end q-mb-sm"><q-btn v-if="canWrite" color="primary" unelevated no-caps icon="o_add" label="New zone" @click="onAdd" /></div>
    <AppDataTable page-key="shipping-zones" row-key="id" :rows="rows" :columns="columns" :loading="loading" :pagination="pagination" show-actions @request="onRequest">
      <template #body-cell-postal="cell"><q-td :props="cell">{{ cell.row.postalCodeStart ? `${cell.row.postalCodeStart}–${cell.row.postalCodeEnd || ''}` : '—' }}</q-td></template>
      <template #body-cell-isEnabled="cell"><q-td :props="cell"><q-badge :color="cell.row.isEnabled ? 'positive' : 'grey'" :label="cell.row.isEnabled ? 'On' : 'Off'" /></q-td></template>
      <template #actions="{ row }">
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)" /><q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)" />
      </template>
    </AppDataTable>
    <ShippingZoneFormDrawer v-model="drawerOpen" :item="editing" :saving="saving" @submit="onSubmit" @cancel="drawerOpen = false" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { shippingZoneApi } from 'modules/shipping/api'
import AppDataTable from 'components/common/AppDataTable.vue'
import ShippingZoneFormDrawer from 'modules/shipping/components/ShippingZoneFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Stores.Write'))
const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'countryCode', label: 'Country', field: 'countryCode', align: 'left' },
  { name: 'region', label: 'Region', field: (r) => r.region || '—', align: 'left' },
  { name: 'postal', label: 'Postal range', field: 'postalCodeStart', align: 'left' },
  { name: 'isEnabled', label: 'Status', field: 'isEnabled', align: 'center' }
]
const rows = ref([]); const loading = ref(false); const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
const drawerOpen = ref(false); const editing = ref(null); const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value; loading.value = true
  try {
    const r = await shippingZoneApi.list({ page: p.page, pageSize: p.rowsPerPage })
    const items = Array.isArray(r) ? r : r?.items || []
    rows.value = items; pagination.value = { ...p, rowsNumber: Array.isArray(r) ? items.length : r?.totalCount ?? items.length }
  } catch (e) { rows.value = []; notify.error(getApiErrorMessage(e)) } finally { loading.value = false }
}
function onRequest (props) { fetch(props) }
function reload () { fetch({ pagination: { ...pagination.value, page: 1 } }) }
function onAdd () { editing.value = null; drawerOpen.value = true }
function onEdit (row) { editing.value = { ...row }; drawerOpen.value = true }
async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value?.id) { await shippingZoneApi.update(editing.value.id, payload); notify.success('Zone updated') }
    else { await shippingZoneApi.create(payload); notify.success('Zone created') }
    drawerOpen.value = false; reload()
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { saving.value = false }
}
async function onDelete (row) {
  if (!(await deleteConfirmation(`the shipping zone "${row.name}"`))) return
  try { await shippingZoneApi.remove(row.id); notify.success('Zone deleted'); reload() } catch (e) { notify.error(getApiErrorMessage(e)) }
}
onMounted(() => fetch())
</script>
