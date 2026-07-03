<template>
  <q-table
    class="app-table-card"
    flat
    bordered
    :rows="rows"
    :columns="computedColumns"
    :row-key="rowKey"
    :loading="loading"
    :pagination="pagination"
    :selection="selectable ? 'multiple' : 'none'"
    :selected="selected"
    binary-state-sort
    v-bind="$attrs"
    @request="onRequest"
    @update:selected="$emit('update:selected', $event)"
  >
    <!-- Forward all parent-provided slots (body-cell-*, header, ...). -->
    <template v-for="name in forwardedSlots" #[name]="slotProps" :key="name">
      <slot :name="name" v-bind="slotProps || {}" />
    </template>

    <!-- Standardized card header: a parent `top` slot wins; otherwise a title + refresh. -->
    <template v-if="hasSlot('top') || title" #top="slotProps">
      <slot name="top" v-bind="slotProps || {}">
        <div class="row items-center full-width no-wrap">
          <div class="app-section__title col ellipsis">{{ title }}</div>
          <q-btn flat round dense icon="o_refresh" :aria-label="refreshLabel" @click="$emit('refresh')">
            <q-tooltip>{{ refreshLabel }}</q-tooltip>
          </q-btn>
        </div>
      </slot>
    </template>

    <!-- Actions column cell -->
    <template v-if="showActions" #body-cell-actions="cellProps">
      <q-td :props="cellProps" class="text-right">
        <slot name="actions" :row="cellProps.row" :props="cellProps">
          <q-btn flat round dense icon="o_more_vert" :aria-label="actionsLabel">
            <q-menu auto-close>
              <slot name="actions-menu" :row="cellProps.row" />
            </q-menu>
          </q-btn>
        </slot>
      </q-td>
    </template>

    <template v-if="!hasSlot('loading')" #loading>
      <q-inner-loading showing color="primary" />
    </template>

    <template v-if="!hasSlot('no-data')" #no-data>
      <div class="full-width row flex-center text-grey-7 q-py-lg">
        <q-icon name="o_inbox" size="22px" class="q-mr-sm" />
        {{ noDataLabel }}
      </div>
    </template>
  </q-table>
</template>

<script setup>
/*
 * AppDataTable (WO-94 Step 10): a q-table wrapper with server-side pagination
 * (@request), optional multi-select, an Actions column, and transparent
 * body-cell-* slot passthrough. Per-page rows-per-page is remembered by
 * page-key via usePreferences.
 */
import { computed, useSlots } from 'vue'
import { usePreferences } from 'composables/usePreferences'

defineOptions({ inheritAttrs: false })

const props = defineProps({
  pageKey: { type: String, default: 'app-table' },
  rows: { type: Array, default: () => [] },
  columns: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  rowKey: { type: String, default: 'id' },
  pagination: {
    type: Object,
    default: () => ({ page: 1, rowsPerPage: 10, rowsNumber: 0 })
  },
  selectable: { type: Boolean, default: false },
  selected: { type: Array, default: () => [] },
  showActions: { type: Boolean, default: false },
  actionsLabel: { type: String, default: 'Actions' },
  noDataLabel: { type: String, default: 'No data available' },
  title: { type: String, default: '' },
  refreshLabel: { type: String, default: 'Refresh' }
})

const emit = defineEmits(['request', 'update:pagination', 'update:selected', 'refresh'])

const slots = useSlots()
function hasSlot (name) {
  return Object.prototype.hasOwnProperty.call(slots, name)
}
const forwardedSlots = computed(() =>
  Object.keys(slots).filter((name) => name !== 'actions' && name !== 'actions-menu' && name !== 'top')
)

const prefs = usePreferences(props.pageKey)

const computedColumns = computed(() => {
  const cols = [...(props.columns || [])]
  if (props.showActions && !cols.some((c) => c.name === 'actions')) {
    cols.push({
      name: 'actions',
      label: props.actionsLabel,
      field: 'actions',
      align: 'right',
      sortable: false
    })
  }
  return cols
})

function onRequest (reqProps) {
  if (reqProps && reqProps.pagination) {
    emit('update:pagination', reqProps.pagination)
    if (reqProps.pagination.rowsPerPage) {
      prefs.pageSize.value = reqProps.pagination.rowsPerPage
      prefs.save()
    }
  }
  emit('request', reqProps)
}
</script>
