<template>
  <div class="app-list-header q-mb-md">
    <q-breadcrumbs
      v-if="breadcrumbs.length"
      class="text-grey-7 q-mb-xs"
      active-color="primary"
      gutter="xs"
    >
      <q-breadcrumbs-el
        v-for="(crumb, i) in breadcrumbs"
        :key="i"
        :label="crumb.label"
        :icon="crumb.icon"
        :to="crumb.to"
      />
    </q-breadcrumbs>

    <div class="row items-center no-wrap q-gutter-sm">
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
        <div class="app-page-title">{{ title }}</div>
        <div v-if="subtitle" class="text-muted text-body2">{{ subtitle }}</div>
      </div>

      <slot name="actions" />

      <slot name="add">
        <q-btn
          v-if="showAdd"
          unelevated
          color="primary"
          icon="o_add"
          :label="addLabel"
          no-caps
          @click="$emit('add')"
        />
      </slot>
    </div>
  </div>
</template>

<script setup>
/*
 * AppListHeader (WO-94 Step 10): breadcrumbs + page title, with add/back slots
 * (defaulting to standard buttons that emit `add` / `back`).
 */
defineProps({
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  breadcrumbs: { type: Array, default: () => [] },
  showAdd: { type: Boolean, default: false },
  addLabel: { type: String, default: 'Add' },
  showBack: { type: Boolean, default: false }
})

defineEmits(['add', 'back'])
</script>
