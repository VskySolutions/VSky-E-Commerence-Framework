<template>
  <q-card flat bordered>
    <q-card-section class="text-subtitle1 text-weight-medium">Order history</q-card-section>
    <q-separator />

    <q-table
      flat
      :rows="rows"
      :columns="columns"
      row-key="id"
      :loading="loading"
      :pagination="pagination"
      :rows-per-page-options="[10, 20, 50]"
      no-data-label="You haven't placed any orders yet."
      @request="onRequest"
    >
      <template #body-cell-status="props">
        <q-td :props="props">
          <q-badge :color="statusColor(props.value)" :label="props.value" />
        </q-td>
      </template>
      <template #body-cell-total="props">
        <q-td :props="props" class="text-weight-medium">{{ formatPrice(props.value) }}</q-td>
      </template>
      <template #body-cell-placedOnUtc="props">
        <q-td :props="props">{{ formatDate(props.value) }}</q-td>
      </template>
    </q-table>
  </q-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import { accountApi } from 'modules/storefront/account-api'
import { formatPrice } from 'modules/storefront/api'
import { getApiErrorMessage } from 'services/api'

const $q = useQuasar()

const rows = ref([])
const loading = ref(false)
const pagination = ref({ page: 1, rowsPerPage: 20, rowsNumber: 0 })

const columns = [
  { name: 'orderNumber', label: 'Order #', field: 'orderNumber', align: 'left' },
  { name: 'placedOnUtc', label: 'Date', field: 'placedOnUtc', align: 'left' },
  { name: 'itemCount', label: 'Items', field: 'itemCount', align: 'center' },
  { name: 'total', label: 'Total', field: 'totalAmount', align: 'right' },
  { name: 'status', label: 'Status', field: 'status', align: 'left' }
]

function formatDate (value) {
  if (!value) return '—'
  try {
    return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
  } catch (e) {
    return value
  }
}

function statusColor (status) {
  const s = (status || '').toLowerCase()
  if (s.includes('cancel')) return 'negative'
  if (s.includes('deliver') || s.includes('complete')) return 'positive'
  if (s.includes('ship')) return 'teal'
  if (s.includes('process') || s.includes('pending')) return 'orange'
  return 'grey'
}

async function fetchPage (page, rowsPerPage) {
  loading.value = true
  try {
    const data = await accountApi.orders({ page, pageSize: rowsPerPage })
    rows.value = Array.isArray(data?.items) ? data.items : []
    pagination.value = {
      page: data?.pageNumber ?? page,
      rowsPerPage,
      rowsNumber: data?.totalCount ?? rows.value.length
    }
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    loading.value = false
  }
}

function onRequest (props) {
  const { page, rowsPerPage } = props.pagination
  fetchPage(page, rowsPerPage)
}

onMounted(() => fetchPage(pagination.value.page, pagination.value.rowsPerPage))
</script>
