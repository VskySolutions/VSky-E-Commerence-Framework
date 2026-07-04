<template>
  <q-page class="app-page">
    <AppDetailHeader
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Orders', to: '/orders' },
        { label: order ? order.orderNumber : 'Order' }
      ]"
      :status="order ? order.status : ''"
      :status-color="order ? statusColor(order.status) : 'grey'"
      @back="$router.push({ name: 'admin-orders' })"
    >
      <template #actions>
        <q-btn outline color="grey-8" no-caps icon="o_download" label="Invoice" class="q-mr-xs" @click="downloadInvoice" />
        <q-btn outline color="grey-8" no-caps icon="o_description" label="Packing slip" @click="downloadPackingSlip" />
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" />

    <div v-if="order" class="row q-col-gutter-md">
      <!-- Left: summary + lines + timeline + shipments -->
      <div class="col-12 col-md-8">
        <AppSection title="Summary">
          <div class="row q-col-gutter-md text-body2">
            <div class="col-12 col-sm-6">
              <div class="text-caption text-grey-7">Customer</div>
              <div class="text-weight-medium">{{ order.contactName || '—' }}</div>
              <div class="text-grey-8">{{ order.contactEmail || '' }}</div>
            </div>
            <div class="col-12 col-sm-6">
              <div class="text-caption text-grey-7">Placed</div>
              <div>{{ formatDate(order.placedOnUtc, true) }}</div>
              <div class="text-caption text-grey-7 q-mt-xs">Total</div>
              <div class="text-h6">{{ formatMoney(order.totalAmount) }}</div>
            </div>
            <div class="col-12">
              <div class="text-caption text-grey-7">Ship to</div>
              <div>{{ shipAddress }}</div>
            </div>
          </div>
        </AppSection>

        <AppSection title="Line items" class="q-mt-md">
          <q-markup-table flat dense>
            <thead>
              <tr>
                <th class="text-left">Product</th>
                <th class="text-left">SKU</th>
                <th class="text-right">Qty</th>
                <th class="text-right">Shipped</th>
                <th class="text-right">Unit</th>
                <th class="text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="l in order.lines" :key="l.id">
                <td>{{ l.productName }}</td>
                <td>{{ l.sku || '—' }}</td>
                <td class="text-right">{{ l.quantity }}</td>
                <td class="text-right">{{ shippedFor(l.id) }}</td>
                <td class="text-right">{{ formatMoney(l.unitPrice) }}</td>
                <td class="text-right">{{ formatMoney(l.lineTotal) }}</td>
              </tr>
            </tbody>
          </q-markup-table>
        </AppSection>

        <AppSection title="Shipments" class="q-mt-md">
          <template #actions>
            <q-btn v-if="canShipMore" color="primary" unelevated no-caps size="sm" icon="o_local_shipping" label="Create shipment" @click="openShipmentDialog" />
          </template>
          <div v-if="!shipments.length" class="text-grey-6">No shipments yet.</div>
          <q-list v-else separator>
            <q-item v-for="s in shipments" :key="s.id">
              <q-item-section>
                <q-item-label class="text-weight-medium">{{ s.shipmentNumber }} <q-badge class="q-ml-sm" :color="statusColor(s.status)" :label="s.status" /></q-item-label>
                <q-item-label caption>
                  {{ s.carrier }}<span v-if="s.serviceName"> · {{ s.serviceName }}</span>
                  <span v-if="s.trackingNumber"> · {{ s.trackingNumber }}</span>
                  · {{ s.lines.reduce((n, x) => n + x.quantity, 0) }} items
                </q-item-label>
              </q-item-section>
              <q-item-section side>
                <q-btn v-if="s.labelPdfUrl" flat dense no-caps size="sm" icon="o_download" label="Label" type="a" :href="s.labelPdfUrl" target="_blank" />
                <q-btn v-else flat dense no-caps size="sm" icon="o_label" label="Generate label" :loading="labelBusy[s.id]" @click="generateLabel(s)" />
              </q-item-section>
            </q-item>
          </q-list>
        </AppSection>

        <AppSection title="Timeline" class="q-mt-md">
          <div v-if="!history.length" class="text-grey-6">No status changes yet.</div>
          <q-timeline v-else color="primary">
            <q-timeline-entry
              v-for="h in history"
              :key="h.id"
              :title="`${h.fromStatus} → ${h.toStatus}`"
              :subtitle="formatDate(h.changedOnUtc, true)"
            >
              <div v-if="h.note" class="text-body2">{{ h.note }}</div>
            </q-timeline-entry>
          </q-timeline>
        </AppSection>
      </div>

      <!-- Right: action panel -->
      <div class="col-12 col-md-4">
        <AppSection title="Actions">
          <div class="column q-gutter-sm">
            <q-btn
              v-for="t in transitions"
              :key="t"
              :color="t === 'Cancelled' ? 'negative' : 'primary'"
              :outline="t === 'Cancelled'"
              unelevated
              no-caps
              :icon="transitionIcon(t)"
              :label="`Mark as ${t}`"
              @click="onTransition(t)"
            />
            <div v-if="!transitions.length" class="text-grey-6 text-caption">No further status changes available.</div>
            <q-separator class="q-my-sm" />
            <q-btn outline color="deep-orange" no-caps icon="o_currency_exchange" label="Refund" @click="openRefundDialog" />
          </div>
        </AppSection>
      </div>
    </div>

    <!-- Tracking dialog (Ship transition) -->
    <q-dialog v-model="trackingDialog.open">
      <q-card style="min-width: 380px">
        <q-card-section class="text-subtitle1 text-weight-medium">Mark as shipped</q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-input v-model="trackingDialog.carrier" dense outlined label="Carrier" :rules="[(v) => !!v || 'Required']" />
          <q-input v-model="trackingDialog.trackingNumber" dense outlined label="Tracking number" :rules="[(v) => !!v || 'Required']" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat no-caps label="Cancel" v-close-popup />
          <q-btn color="primary" unelevated no-caps label="Ship" :loading="busy" @click="confirmShip" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Create shipment dialog -->
    <q-dialog v-model="shipmentDialog.open">
      <q-card style="min-width: 460px; max-width: 95vw">
        <q-card-section class="text-subtitle1 text-weight-medium">Create shipment</q-card-section>
        <q-card-section class="q-gutter-sm scroll" style="max-height: 60vh">
          <div v-for="row in shipmentDialog.lines" :key="row.orderLineItemId" class="row items-center q-col-gutter-sm">
            <div class="col">
              <div class="text-body2">{{ row.productName }}</div>
              <div class="text-caption text-grey-6">Remaining: {{ row.remaining }}</div>
            </div>
            <div class="col-4">
              <q-input v-model.number="row.quantity" type="number" dense outlined min="0" :max="row.remaining" />
            </div>
          </div>
          <q-separator class="q-my-sm" />
          <q-input v-model="shipmentDialog.carrier" dense outlined label="Carrier" :rules="[(v) => !!v || 'Required']" />
          <q-input v-model="shipmentDialog.trackingNumber" dense outlined label="Tracking number (optional)" />
          <q-input v-model="shipmentDialog.serviceName" dense outlined label="Service (optional)" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat no-caps label="Cancel" v-close-popup />
          <q-btn color="primary" unelevated no-caps label="Create shipment" :loading="busy" @click="confirmShipment" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Refund dialog -->
    <q-dialog v-model="refundDialog.open">
      <q-card style="min-width: 420px">
        <q-card-section class="text-subtitle1 text-weight-medium">Refund order</q-card-section>
        <q-card-section class="q-gutter-sm">
          <q-option-group v-model="refundDialog.mode" :options="[{ label: 'Selected line items', value: 'lines' }, { label: 'Custom amount', value: 'amount' }, { label: 'Full remaining balance', value: 'full' }]" />
          <template v-if="refundDialog.mode === 'lines'">
            <q-option-group
              v-model="refundDialog.lineIds"
              type="checkbox"
              :options="order.lines.map((l) => ({ label: `${l.productName} — ${formatMoney(l.lineTotal)}`, value: l.id }))"
            />
          </template>
          <q-input v-if="refundDialog.mode === 'amount'" v-model.number="refundDialog.amount" type="number" dense outlined label="Amount" step="0.01" />
          <q-input v-model="refundDialog.reason" dense outlined label="Reason (optional)" />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat no-caps label="Cancel" v-close-popup />
          <q-btn color="deep-orange" unelevated no-caps label="Process refund" :loading="busy" @click="confirmRefund" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
/*
 * Admin order detail (WO-114): summary, line items (with shipped counts), status
 * timeline, context-aware status actions, invoice/packing-slip download, partial
 * shipments (create + label), and refunds — all against the existing admin APIs.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import {
  orderApi, orderStatusColor as statusColor, allowedTransitions, formatMoney, formatDate
} from 'modules/orders/api'

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const notify = useNotify()

const order = ref(null)
const history = ref([])
const shipments = ref([])
const loading = ref(false)
const busy = ref(false)
const labelBusy = reactive({})

const trackingDialog = reactive({ open: false, carrier: '', trackingNumber: '' })
const shipmentDialog = reactive({ open: false, lines: [], carrier: '', trackingNumber: '', serviceName: '' })
const refundDialog = reactive({ open: false, mode: 'lines', lineIds: [], amount: null, reason: '' })

const transitions = computed(() => (order.value ? allowedTransitions(order.value.status) : []))
const shipAddress = computed(() => {
  const o = order.value
  if (!o) return '—'
  return [o.addressLine1, o.addressLine2, o.city, o.stateProvince || o.region, o.postalCode, o.countryCode].filter(Boolean).join(', ') || '—'
})
const canShipMore = computed(() => (order.value?.lines || []).some((l) => remainingFor(l.id) > 0))

function shippedFor (lineId) {
  return shipments.value.reduce((sum, s) => sum + s.lines.filter((x) => x.orderLineItemId === lineId).reduce((n, x) => n + x.quantity, 0), 0)
}
function remainingFor (lineId) {
  const line = (order.value?.lines || []).find((l) => l.id === lineId)
  return line ? line.quantity - shippedFor(lineId) : 0
}

function transitionIcon (t) {
  return { Processing: 'o_settings', Shipped: 'o_local_shipping', Delivered: 'o_check_circle', Cancelled: 'o_cancel' }[t] || 'o_arrow_forward'
}

async function load () {
  loading.value = true
  try {
    const [o, h, s] = await Promise.all([
      orderApi.get(route.params.id),
      orderApi.history(route.params.id).catch(() => []),
      orderApi.shipments(route.params.id).catch(() => [])
    ])
    order.value = o
    history.value = Array.isArray(h) ? h : []
    shipments.value = Array.isArray(s) ? s : []
  } catch (err) {
    notify.error(getApiErrorMessage(err))
    order.value = null
  } finally {
    loading.value = false
  }
}

function onTransition (t) {
  if (t === 'Shipped') {
    trackingDialog.carrier = ''
    trackingDialog.trackingNumber = ''
    trackingDialog.open = true
    return
  }
  if (t === 'Cancelled') {
    $q.dialog({ title: 'Cancel order', message: 'Cancel this order? Any authorized payment will be released.', cancel: true, ok: { label: 'Cancel order', color: 'negative' } })
      .onOk(() => advance(t))
    return
  }
  advance(t)
}

async function advance (toStatus, extra = {}) {
  busy.value = true
  try {
    await orderApi.advance(route.params.id, { toStatus, ...extra })
    notify.success(`Order marked ${toStatus}`)
    trackingDialog.open = false
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    busy.value = false
  }
}

function confirmShip () {
  if (!trackingDialog.carrier || !trackingDialog.trackingNumber) { notify.warning('Carrier and tracking number are required'); return }
  advance('Shipped', { carrier: trackingDialog.carrier, trackingNumber: trackingDialog.trackingNumber })
}

function openShipmentDialog () {
  shipmentDialog.lines = (order.value.lines || [])
    .map((l) => ({ orderLineItemId: l.id, productName: l.productName, remaining: remainingFor(l.id), quantity: remainingFor(l.id) }))
    .filter((r) => r.remaining > 0)
  shipmentDialog.carrier = ''
  shipmentDialog.trackingNumber = ''
  shipmentDialog.serviceName = ''
  shipmentDialog.open = true
}

async function confirmShipment () {
  const lines = shipmentDialog.lines.filter((r) => r.quantity > 0).map((r) => ({ orderLineItemId: r.orderLineItemId, quantity: r.quantity }))
  if (!lines.length) { notify.warning('Select at least one item to ship'); return }
  if (!shipmentDialog.carrier) { notify.warning('Carrier is required'); return }
  busy.value = true
  try {
    await orderApi.createShipment(route.params.id, {
      lines, carrier: shipmentDialog.carrier, trackingNumber: shipmentDialog.trackingNumber || null, serviceName: shipmentDialog.serviceName || null
    })
    notify.success('Shipment created')
    shipmentDialog.open = false
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    busy.value = false
  }
}

async function generateLabel (s) {
  labelBusy[s.id] = true
  try {
    await orderApi.generateLabel(s.id)
    notify.success('Label generated')
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    labelBusy[s.id] = false
  }
}

function openRefundDialog () {
  refundDialog.mode = 'lines'
  refundDialog.lineIds = []
  refundDialog.amount = null
  refundDialog.reason = ''
  refundDialog.open = true
}

async function confirmRefund () {
  const payload = { reason: refundDialog.reason || null }
  if (refundDialog.mode === 'amount') payload.amount = Number(refundDialog.amount) || 0
  else if (refundDialog.mode === 'lines') payload.orderLineItemIds = refundDialog.lineIds
  // 'full' → send neither → backend refunds the full remaining balance
  busy.value = true
  try {
    await orderApi.refund(route.params.id, payload)
    notify.success('Refund processed')
    refundDialog.open = false
    await load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    busy.value = false
  }
}

function downloadInvoice () { orderApi.invoice(route.params.id).catch((e) => notify.error(getApiErrorMessage(e))) }
function downloadPackingSlip () { orderApi.packingSlip(route.params.id).catch((e) => notify.error(getApiErrorMessage(e))) }

onMounted(load)
</script>
