<template>
  <div>
    <div class="row justify-end q-mb-sm"><q-btn v-if="canWrite" color="primary" unelevated no-caps icon="o_add" label="New method" @click="onAdd" /></div>
    <AppDataTable page-key="shipping-methods" row-key="id" :rows="rows" :columns="columns" :loading="loading" :pagination="pagination" show-actions @request="onRequest">
      <template #body-cell-isEnabled="cell"><q-td :props="cell"><q-badge :color="cell.row.isEnabled ? 'positive' : 'grey'" :label="cell.row.isEnabled ? 'On' : 'Off'" /></q-td></template>
      <template #actions="{ row }">
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)" /><q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)" />
      </template>
    </AppDataTable>
    <ShippingMethodFormDrawer v-model="drawerOpen" :item="editing" :saving="saving" @submit="onSubmit" @cancel="drawerOpen = false" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { shippingMethodApi } from 'modules/shipping/api'
import AppDataTable from 'components/common/AppDataTable.vue'
import ShippingMethodFormDrawer from 'modules/shipping/components/ShippingMethodFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Stores.Write'))
const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'methodType', label: 'Type', field: 'methodType', align: 'left' },
  { name: 'flatRate', label: 'Flat rate', field: (r) => r.flatRate ?? '—', align: 'right' },
  { name: 'displayOrder', label: 'Order', field: 'displayOrder', align: 'right' },
  { name: 'isEnabled', label: 'Status', field: 'isEnabled', align: 'center' }
]
const rows = ref([]); const loading = ref(false); const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
const drawerOpen = ref(false); const editing = ref(null); const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value; loading.value = true
  try {
    const r = await shippingMethodApi.list({ page: p.page, pageSize: p.rowsPerPage })
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
    if (editing.value?.id) { await shippingMethodApi.update(editing.value.id, payload); notify.success('Method updated') }
    else { await shippingMethodApi.create(payload); notify.success('Method created') }
    drawerOpen.value = false; reload()
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { saving.value = false }
}
async function onDelete (row) {
  if (!(await deleteConfirmation(`the shipping method "${row.name}"`))) return
  try { await shippingMethodApi.remove(row.id); notify.success('Method deleted'); reload() } catch (e) { notify.error(getApiErrorMessage(e)) }
}
onMounted(() => fetch())
</script>
