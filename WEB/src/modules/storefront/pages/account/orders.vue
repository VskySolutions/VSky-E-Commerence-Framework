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
      <template #body-cell-actions="props">
        <q-td :props="props" class="text-right">
          <q-btn flat round dense color="primary" icon="o_visibility" @click="view(props.row)">
            <q-tooltip>View details</q-tooltip>
          </q-btn>
          <q-btn
            flat
            round
            dense
            color="primary"
            icon="o_download"
            :loading="downloadingId === props.row.id"
            @click="downloadInvoice(props.row)"
          >
            <q-tooltip>Download invoice</q-tooltip>
          </q-btn>
        </q-td>
      </template>
    </q-table>

    <!-- Order details dialog -->
    <q-dialog v-model="detail.open">
      <q-card style="width: 720px; max-width: 95vw">
        <q-card-section class="row items-center justify-between">
          <div>
            <div class="text-subtitle1 text-weight-bold">Order {{ detail.data ? detail.data.orderNumber : '' }}</div>
            <div v-if="detail.data" class="text-caption text-grey-6">Placed {{ formatDate(detail.data.placedOnUtc) }}</div>
          </div>
          <q-badge v-if="detail.data" :color="statusColor(detail.data.status)" :label="detail.data.status" />
        </q-card-section>
        <q-separator />

        <q-card-section class="scroll" style="max-height: 70vh">
          <q-inner-loading :showing="detail.loading" />
          <template v-if="detail.data">
            <!-- Ship-to -->
            <div class="text-body2 text-weight-medium q-mb-xs">Delivery address</div>
            <div class="text-body2 text-grey-8 q-mb-md">
              <div v-if="detail.data.contactName">{{ detail.data.contactName }}</div>
              <div>{{ addressLine(detail.data) }}</div>
              <div v-if="detail.data.contactPhone" class="text-grey-6">{{ detail.data.contactPhone }}</div>
              <div v-if="detail.data.contactEmail" class="text-grey-6">{{ detail.data.contactEmail }}</div>
            </div>

            <!-- Line items -->
            <q-markup-table flat bordered dense class="q-mb-md">
              <thead>
                <tr>
                  <th class="text-left">Product</th>
                  <th class="text-center">Qty</th>
                  <th class="text-right">Unit price</th>
                  <th class="text-right">Total</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="line in detail.data.lines" :key="line.id">
                  <td class="text-left">
                    {{ line.productName }}
                    <div v-if="line.sku" class="text-caption text-grey-6">SKU: {{ line.sku }}</div>
                  </td>
                  <td class="text-center">{{ line.quantity }}</td>
                  <td class="text-right">{{ formatPrice(line.unitPrice) }}</td>
                  <td class="text-right">{{ formatPrice(line.lineTotal) }}</td>
                </tr>
              </tbody>
            </q-markup-table>

            <div class="row justify-end items-center q-gutter-sm">
              <span class="text-body1 text-weight-medium">Total</span>
              <span class="text-h6 text-weight-bold">{{ formatPrice(detail.data.totalAmount) }}</span>
            </div>
          </template>
        </q-card-section>

        <q-separator />
        <q-card-actions align="right">
          <q-btn flat no-caps label="Close" v-close-popup />
          <q-btn
            color="primary"
            unelevated
            no-caps
            icon="o_download"
            label="Download invoice"
            :loading="detail.data && downloadingId === detail.data.id"
            :disable="!detail.data"
            @click="downloadInvoice(detail.data)"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-card>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import { accountApi } from 'modules/storefront/account-api'
import { formatPrice } from 'modules/storefront/api'
import { formatDateTime as formatDate } from 'src/utils/datetime'
import { getApiErrorMessage } from 'services/api'

const $q = useQuasar()

const rows = ref([])
const loading = ref(false)
const downloadingId = ref(null)
const pagination = ref({ page: 1, rowsPerPage: 20, rowsNumber: 0 })
const detail = reactive({ open: false, loading: false, data: null })

const columns = [
  { name: 'orderNumber', label: 'Order #', field: 'orderNumber', align: 'left' },
  { name: 'placedOnUtc', label: 'Date', field: 'placedOnUtc', align: 'left' },
  { name: 'itemCount', label: 'Items', field: 'itemCount', align: 'center' },
  { name: 'total', label: 'Total', field: 'totalAmount', align: 'right' },
  { name: 'status', label: 'Status', field: 'status', align: 'left' },
  { name: 'actions', label: 'Actions', field: 'id', align: 'right' }
]

function addressLine (o) {
  return [o.addressLine1, o.addressLine2, o.landmark, o.city, o.stateProvince || o.region, o.postalCode, o.countryCode]
    .filter(Boolean)
    .join(', ')
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

async function view (row) {
  detail.open = true
  detail.loading = true
  detail.data = null
  try {
    detail.data = await accountApi.getOrder(row.id)
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    detail.loading = false
  }
}

async function downloadInvoice (row) {
  if (!row) return
  downloadingId.value = row.id
  try {
    await accountApi.downloadInvoice(row.id)
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    downloadingId.value = null
  }
}

onMounted(() => fetchPage(pagination.value.page, pagination.value.rowsPerPage))
</script>
