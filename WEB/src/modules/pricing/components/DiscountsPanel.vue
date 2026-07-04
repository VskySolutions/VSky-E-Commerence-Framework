<template>
  <div>
    <div class="row items-center justify-end q-mb-sm">
      <q-btn v-if="canWrite" color="primary" unelevated no-caps icon="o_add" label="New discount" @click="onAdd" />
    </div>
    <AppDataTable page-key="pricing-discounts" row-key="id" :rows="rows" :columns="columns" :loading="loading" :pagination="pagination" show-actions @request="onRequest">
      <template #body-cell-value="cell">
        <q-td :props="cell">{{ discountValueLabel(cell.row) }}</q-td>
      </template>
      <template #body-cell-isActive="cell">
        <q-td :props="cell"><q-badge :color="cell.row.isActive ? 'positive' : 'grey'" :label="cell.row.isActive ? 'Active' : 'Off'" /></q-td>
      </template>
      <template #actions="{ row }">
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)"><q-tooltip>Edit</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)"><q-tooltip>Delete</q-tooltip></q-btn>
      </template>
    </AppDataTable>
    <DiscountFormDrawer v-model="drawerOpen" :item="editing" :saving="saving" @submit="onSubmit" @cancel="drawerOpen = false" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { discountApi, discountValueLabel } from 'modules/pricing/api'
import AppDataTable from 'components/common/AppDataTable.vue'
import DiscountFormDrawer from 'modules/pricing/components/DiscountFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'scope', label: 'Scope', field: 'scope', align: 'left' },
  { name: 'type', label: 'Type', field: 'type', align: 'left' },
  { name: 'value', label: 'Value', field: 'value', align: 'right' },
  { name: 'isActive', label: 'Status', field: 'isActive', align: 'center' }
]
const rows = ref([])
const loading = ref(false)
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await discountApi.list({ page: p.page, pageSize: p.rowsPerPage })
    const items = Array.isArray(result) ? result : result?.items || []
    rows.value = items
    pagination.value = { ...p, rowsNumber: Array.isArray(result) ? items.length : result?.totalCount ?? items.length }
  } catch (err) { rows.value = []; notify.error(getApiErrorMessage(err)) } finally { loading.value = false }
}
function onRequest (props) { fetch(props) }
function reload () { fetch({ pagination: { ...pagination.value, page: 1 } }) }
function onAdd () { editing.value = null; drawerOpen.value = true }
function onEdit (row) { editing.value = { ...row }; drawerOpen.value = true }
async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value?.id) { await discountApi.update(editing.value.id, payload); notify.success('Discount updated') }
    else { await discountApi.create(payload); notify.success('Discount created') }
    drawerOpen.value = false; reload()
  } catch (err) { notify.error(getApiErrorMessage(err)) } finally { saving.value = false }
}
async function onDelete (row) {
  if (!(await deleteConfirmation(`the discount "${row.name}"`))) return
  try { await discountApi.remove(row.id); notify.success('Discount deleted'); reload() } catch (err) { notify.error(getApiErrorMessage(err)) }
}
onMounted(() => fetch())
</script>
