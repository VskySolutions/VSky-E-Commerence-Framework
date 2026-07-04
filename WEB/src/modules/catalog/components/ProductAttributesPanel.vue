<template>
  <div>
    <div class="row items-center justify-between q-mb-sm">
      <q-input
        v-model="search"
        dense
        outlined
        debounce="400"
        placeholder="Search attributes"
        style="max-width: 280px"
        @update:model-value="reload"
      >
        <template #prepend><q-icon name="o_search" /></template>
      </q-input>
      <q-btn v-if="canWrite" color="primary" unelevated no-caps icon="o_add" label="Add attribute" @click="onAdd" />
    </div>

    <AppDataTable
      page-key="catalog-product-attributes"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-displayType="cell">
        <q-td :props="cell"><q-badge outline color="primary" :label="cell.row.displayType" /></q-td>
      </template>
      <template #body-cell-valuesCount="cell">
        <q-td :props="cell">{{ (cell.row.values || []).length }}</q-td>
      </template>
      <template #body-cell-inUseCount="cell">
        <q-td :props="cell">
          <span :class="cell.row.inUseCount > 0 ? 'text-weight-medium' : 'text-grey-6'">{{ cell.row.inUseCount || 0 }}</span>
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)"><q-tooltip>Edit</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)"><q-tooltip>Delete</q-tooltip></q-btn>
      </template>
    </AppDataTable>

    <ProductAttributeFormDrawer
      v-model="drawerOpen"
      :item="editing"
      :saving="saving"
      @submit="onSubmit"
      @cancel="drawerOpen = false"
    />
  </div>
</template>

<script setup>
/*
 * Product Attributes panel (WO-15): the global product-attribute library list +
 * CRUD (create/edit via ProductAttributeFormDrawer, delete with in-use protection).
 */
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { productAttributeApi } from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import AppDataTable from 'components/common/AppDataTable.vue'
import ProductAttributeFormDrawer from 'modules/catalog/components/ProductAttributeFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const columns = [
  { name: 'name', label: 'Attribute Name', field: 'name', align: 'left' },
  { name: 'displayType', label: 'Display Type', field: 'displayType', align: 'left' },
  { name: 'valuesCount', label: 'Values', field: (r) => (r.values || []).length, align: 'center' },
  { name: 'inUseCount', label: 'In Use', field: 'inUseCount', align: 'center' }
]

const rows = ref([])
const loading = ref(false)
const search = ref('')
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await productAttributeApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      search: search.value || undefined
    })
    const items = Array.isArray(result) ? result : result?.items || []
    const total = Array.isArray(result) ? result.length : result?.totalCount ?? items.length
    rows.value = items
    pagination.value = { ...p, rowsNumber: total }
  } catch (err) {
    rows.value = []
    pagination.value = { ...p, rowsNumber: 0 }
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onRequest (props) { fetch(props) }
function reload () { fetch({ pagination: { ...pagination.value, page: 1 } }) }

async function onAdd () {
  editing.value = null
  drawerOpen.value = true
}

async function onEdit (row) {
  // The list row already carries values; fetch the full record to be safe.
  try {
    editing.value = await productAttributeApi.get(row.id)
  } catch (err) {
    editing.value = { ...row }
  }
  drawerOpen.value = true
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await productAttributeApi.update(editing.value.id, payload)
      notify.success('Attribute updated')
    } else {
      await productAttributeApi.create(payload)
      notify.success('Attribute created')
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
  const inUse = row.inUseCount || 0
  const message = inUse > 0
    ? `the attribute "${row.name}". It is assigned to ${inUse} product${inUse === 1 ? '' : 's'} — deleting it will remove it from ${inUse === 1 ? 'that product' : 'those products'}`
    : `the attribute "${row.name}"`
  if (!(await deleteConfirmation(message))) return
  try {
    await productAttributeApi.remove(row.id)
    notify.success('Attribute deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
