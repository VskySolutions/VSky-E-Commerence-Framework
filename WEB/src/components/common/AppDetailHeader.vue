<template>
  <div class="app-detail-header row items-center no-wrap q-gutter-sm q-mb-md">
    <slot name="back">
      <q-btn
        v-if="showBack"
        flat
        round
        dense
        icon="o_arrow_back"
        aria-label="Back"
        @click="$emit('back')"
      />
    </slot>

    <div class="col">
      <div class="row items-center q-gutter-sm">
        <span class="app-page-title">{{ title }}</span>
        <q-badge v-if="status" :color="statusColor" :label="status" class="q-px-sm" />
      </div>
      <div v-if="subtitle" class="text-muted text-body2">{{ subtitle }}</div>
    </div>

    <slot name="actions" />
  </div>
</template>

<script setup>
/*
 * AppDetailHeader (WO-94 Step 10): title + optional status badge + actions slot
 * for a record detail view.
 */
defineProps({
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  status: { type: String, default: '' },
  statusColor: { type: String, default: 'primary' },
  showBack: { type: Boolean, default: true }
})

defineEmits(['back'])
</script>
