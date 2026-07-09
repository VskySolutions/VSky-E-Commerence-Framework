<template>
  <q-page class="app-page">
    <AppDetailHeader
      :title="isCreate ? 'New product attribute' : (entity?.name || 'Product attribute')"
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Attributes', to: { name: 'catalog-attributes', query: { tab: 'product' } } },
        { label: isCreate ? 'New attribute' : (entity?.name || 'Product attribute') }
      ]"
      show-back
      @back="router.push({ name: 'catalog-attributes', query: { tab: 'product' } })"
    >
      <template #actions>
        <q-chip
          v-if="saveStatus"
          :icon="saveStatus.icon"
          :color="saveStatus.chip"
          :text-color="saveStatus.text"
          square
          dense
          class="q-mr-sm text-caption"
        >
          <q-spinner v-if="saveStatus.spin" size="14px" class="q-mr-xs" />
          {{ saveStatus.label }}
        </q-chip>
      </template>
    </AppDetailHeader>

    <q-inner-loading :showing="loading" color="primary" />

    <q-banner v-if="!loading && !isCreate && !entity" class="bg-grey-2 rounded-borders">
      Attribute not found.
    </q-banner>

    <template v-if="isCreate || entity">
      <div v-if="!isCreate" class="row items-center text-caption text-grey-7 q-mb-sm q-px-xs">
        <q-icon name="o_cloud_sync" size="16px" class="q-mr-xs" />
        Changes are saved automatically as you edit — no need to press save.
      </div>

      <q-card flat bordered class="app-section">
        <q-tabs
          v-model="tab"
          align="left"
          active-color="primary"
          indicator-color="primary"
          class="text-grey-7 app-detail-tabs"
          no-caps
          inline-label
        >
          <q-tab name="general" icon="o_palette" label="General" />
        </q-tabs>

        <q-separator />

        <q-tab-panels v-model="tab" animated keep-alive>
          <q-tab-panel name="general" class="q-gutter-y-sm">
            <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Colour, Size" :disable="!canWrite" />
            <div class="row q-col-gutter-sm">
              <div class="col-12 col-md-6"><AppSelect v-model="form.displayType" label="Display type" :options="attributeDisplayTypeOptions" :disable="!canWrite" /></div>
              <div class="col-6 col-md-3"><AppTextField v-model="form.displayOrder" label="Display order" type="number" :disable="!canWrite" /></div>
            </div>
            <AppTextField v-model="form.description" label="Description" type="textarea" autogrow :disable="!canWrite" />

            <q-separator class="q-my-md" />
            <div class="row items-center justify-between q-mb-sm">
              <AppFieldLabel label="Values" />
              <q-btn v-if="canWrite" flat dense no-caps color="primary" icon="o_add" label="Add value" @click="addValue" />
            </div>
            <div v-if="!form.values.length" class="text-grey-6 text-caption q-mb-sm">No values yet. Add one or more (e.g. Red, Blue, Green).</div>
            <div v-for="(val, i) in form.values" :key="val._key" class="row items-center q-col-gutter-xs q-mb-xs no-wrap">
              <div class="col-auto column">
                <q-btn flat dense round size="sm" icon="o_keyboard_arrow_up" :disable="i === 0 || !canWrite" @click="move(i, -1)" />
                <q-btn flat dense round size="sm" icon="o_keyboard_arrow_down" :disable="i === form.values.length - 1 || !canWrite" @click="move(i, 1)" />
              </div>
              <div v-if="isSwatch" class="col-auto">
                <input type="color" :value="val.colorHex || '#000000'" class="attr-swatch" aria-label="Value colour" :disabled="!canWrite" @input="val.colorHex = $event.target.value">
              </div>
              <div class="col"><q-input v-model="val.value" dense outlined placeholder="Value label" :disable="!canWrite" /></div>
              <div class="col-auto"><q-btn v-if="canWrite" flat dense round size="sm" icon="o_delete" color="negative" @click="removeValue(i)" /></div>
            </div>
          </q-tab-panel>
        </q-tab-panels>

        <template v-if="isCreate">
          <q-separator />
          <q-card-actions class="q-pa-md">
            <div class="text-caption text-grey-7">Create the attribute — changes auto-save from then on.</div>
            <q-space />
            <q-btn v-if="canWrite" unelevated color="primary" no-caps icon="o_check" label="Create attribute" :loading="creating" @click="create" />
          </q-card-actions>
        </template>
      </q-card>
    </template>
  </q-page>
</template>

<script setup>
/* Product attribute create + manage page (full-page auto-save via useDetailForm). Values are part of
 * the form, so add/remove/reorder/edit auto-save via the deep form watch. */
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { usePermissions } from 'composables/usePermissions'
import { useDetailForm } from 'composables/useDetailForm'
import { required, maxLength } from 'validators'
import { productAttributeApi, attributeDisplayTypeOptions } from 'modules/catalog/api'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const router = useRouter()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const tab = ref('general')
let keySeq = 0
const nextKey = () => `v${keySeq++}`

function buildPayload (f) {
  return {
    name: (f.name || '').trim(),
    displayType: f.displayType,
    description: f.description || null,
    displayOrder: Number(f.displayOrder) || 0,
    values: (f.values || [])
      .filter((v) => (v.value || '').trim())
      .map((v, index) => ({
        id: v.id || undefined,
        value: v.value.trim(),
        displayOrder: index,
        colorHex: f.displayType === 'Swatch' ? (v.colorHex || null) : null
      }))
  }
}

const {
  form, v$, entity, loading, creating, isCreate, saveStatus, create
} = useDetailForm({
  createRouteName: 'product-attribute-new',
  detailRouteName: 'product-attribute-detail',
  entityLabel: 'attribute',
  api: productAttributeApi,
  buildPayload,
  empty: { name: '', displayType: 'Dropdown', description: '', displayOrder: 0, values: [] },
  rules: { name: { required, maxLength: maxLength(200) } },
  hydrateForm: (f, e) => {
    f.values = (e.values || []).map((v) => ({ _key: nextKey(), id: v.id || null, value: v.value || '', colorHex: v.colorHex || null }))
  }
})

const isSwatch = computed(() => form.displayType === 'Swatch')
function addValue () { form.values.push({ _key: nextKey(), id: null, value: '', colorHex: isSwatch.value ? '#000000' : null }) }
function removeValue (i) { form.values.splice(i, 1) }
function move (i, dir) {
  const j = i + dir
  if (j < 0 || j >= form.values.length) return
  const [row] = form.values.splice(i, 1)
  form.values.splice(j, 0, row)
}
</script>

<style scoped lang="scss">
.app-detail-tabs {
  :deep(.q-tab) { min-height: 44px; }
}
.attr-swatch {
  width: 34px; height: 34px; border: 1px solid #ddd; border-radius: 4px; padding: 0; cursor: pointer; background: none;
}
</style>
