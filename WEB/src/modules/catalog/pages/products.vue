<template>
  <q-page class="app-page">
    <AppListHeader
      title="Products"
      subtitle="Manage catalog products."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Products' }]"
      :show-add="canWrite"
      add-label="New product"
      @add="onAdd"
    >
      <template #actions>
        <q-input
          v-model="search"
          dense
          outlined
          debounce="400"
          placeholder="Search"
          class="q-mr-sm"
          style="min-width: 200px"
          @update:model-value="reload"
        >
          <template #prepend><q-icon name="o_search" /></template>
        </q-input>
        <q-select
          v-model="typeFilter"
          dense
          outlined
          clearable
          emit-value
          map-options
          :options="productTypeOptions"
          placeholder="Type"
          class="q-mr-sm"
          style="min-width: 160px"
          @update:model-value="reload"
        />
        <q-select
          v-model="publishedFilter"
          dense
          outlined
          emit-value
          map-options
          :options="publishedOptions"
          class="q-mr-sm"
          style="min-width: 150px"
          @update:model-value="reload"
        />
      </template>
    </AppListHeader>

    <AppDataTable
      page-key="catalog-products"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-productType="cell">
        <q-td :props="cell">
          <q-badge outline color="primary" :label="productTypeLabel(cell.row.productType)" />
        </q-td>
      </template>

      <template #body-cell-price="cell">
        <q-td :props="cell">{{ formatPrice(cell.row.price) }}</q-td>
      </template>

      <template #body-cell-isPublished="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.isPublished ? 'positive' : 'grey'"
            :label="cell.row.isPublished ? 'Published' : 'Draft'"
          />
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn flat round dense icon="o_tune" @click="onManage(row)">
          <q-tooltip>Manage details</q-tooltip>
        </q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)">
          <q-tooltip>Edit</q-tooltip>
        </q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)">
          <q-tooltip>Delete</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>

    <ProductFormDrawer
      v-model="drawerOpen"
      :item="editing"
      :saving="saving"
      @submit="onSubmit"
      @cancel="drawerOpen = false"
    />
  </q-page>
</template>

<script setup>
/*
 * Products list page (WO-15): AppListHeader (search + type + published filters)
 * + AppDataTable (server pagination) + ProductFormDrawer. Sub-resources are
 * managed on the product detail page.
 */
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getApiErrorMessage } from 'services/api'
import { productApi, productTypeOptions, productTypeLabel } from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import ProductFormDrawer from 'modules/catalog/components/ProductFormDrawer.vue'

const router = useRouter()
const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'sku', label: 'SKU', field: 'sku', align: 'left' },
  { name: 'productType', label: 'Type', field: 'productType', align: 'left' },
  { name: 'price', label: 'Price', field: 'price', align: 'right' },
  { name: 'stockQuantity', label: 'Stock', field: 'stockQuantity', align: 'right' },
  { name: 'isPublished', label: 'Published', field: 'isPublished', align: 'center' }
]

const publishedOptions = [
  { label: 'All', value: null },
  { label: 'Published', value: true },
  { label: 'Draft', value: false }
]

const rows = ref([])
const loading = ref(false)
const search = ref('')
const typeFilter = ref(null)
const publishedFilter = ref(null)
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })

const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

function formatPrice (value) {
  if (value === null || value === undefined) return '—'
  return Number(value).toFixed(2)
}

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await productApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      search: search.value || undefined,
      type: typeFilter.value || undefined,
      isPublished: publishedFilter.value === null ? undefined : publishedFilter.value
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

function onRequest (props) {
  fetch(props)
}

function reload () {
  fetch({ pagination: { ...pagination.value, page: 1 } })
}

function onAdd () {
  editing.value = null
  drawerOpen.value = true
}

function onEdit (row) {
  editing.value = { ...row }
  drawerOpen.value = true
}

function onManage (row) {
  router.push({ name: 'catalog-product-detail', params: { id: row.id } })
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await productApi.update(editing.value.id, payload)
      notify.success('Product updated')
    } else {
      const created = await productApi.create(payload)
      notify.success('Product created')
      drawerOpen.value = false
      // Send the user straight to the detail page to manage sub-resources.
      if (created && created.id) {
        router.push({ name: 'catalog-product-detail', params: { id: created.id } })
        return
      }
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
  if (!(await deleteConfirmation(`the product "${row.name}"`))) return
  try {
    await productApi.remove(row.id)
    notify.success('Product deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
