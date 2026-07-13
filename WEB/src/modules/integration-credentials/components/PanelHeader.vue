<template>
  <q-card flat bordered class="panel-header q-mb-md">
    <div class="row items-center no-wrap q-px-md q-py-sm">
      <q-icon :name="item.icon" size="26px" class="q-mr-sm text-primary" />
      <div class="col ellipsis-2-lines">
        <div class="text-subtitle1 text-weight-medium">{{ item.label }}</div>
        <div class="text-caption text-grey-7">{{ item.description }}</div>
      </div>
      <div class="row items-center q-gutter-sm no-wrap q-ml-md">
        <slot name="actions" />
      </div>
    </div>
  </q-card>
</template>

<script setup>
/* PanelHeader: card-styled title row (icon + label + description) with an `actions` slot for the panel's
 * buttons (Reload / Add / New / Save). Shared by every Integrations-hub panel for a consistent look. */
defineProps({ item: { type: Object, required: true } })
</script>

<style scoped>
.panel-header {
  border-radius: 8px;
}
</style>
