<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit product attribute' : 'New product attribute'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Colour, Size" />
    <AppSelect v-model="form.displayType" label="Display type" :options="attributeDisplayTypeOptions" />
    <AppTextField v-model="form.description" label="Description" type="textarea" autogrow />
    <AppTextField v-model="form.displayOrder" label="Display order" type="number" />

    <q-separator class="q-my-md" />

    <div class="row items-center justify-between q-mb-sm">
      <AppFieldLabel label="Values" />
      <q-btn flat dense no-caps color="primary" icon="o_add" label="Add value" @click="addValue" />
    </div>

    <div v-if="!form.values.length" class="text-grey-6 text-caption q-mb-sm">
      No values yet. Add one or more (e.g. Red, Blue, Green).
    </div>

    <div v-for="(val, i) in form.values" :key="val._key" class="row items-center q-col-gutter-xs q-mb-xs no-wrap">
      <div class="col-auto column">
        <q-btn flat dense round size="sm" icon="o_keyboard_arrow_up" :disable="i === 0" @click="move(i, -1)" />
        <q-btn flat dense round size="sm" icon="o_keyboard_arrow_down" :disable="i === form.values.length - 1" @click="move(i, 1)" />
      </div>
      <div v-if="isSwatch" class="col-auto">
        <input
          type="color"
          :value="val.colorHex || '#000000'"
          class="attr-swatch"
          aria-label="Value colour"
          @input="val.colorHex = $event.target.value"
        >
      </div>
      <div class="col">
        <q-input v-model="val.value" dense outlined placeholder="Value label" />
      </div>
      <div class="col-auto">
        <q-btn flat dense round size="sm" icon="o_delete" color="negative" @click="removeValue(i)" />
      </div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * ProductAttributeFormDrawer (WO-15): create/edit a global product attribute — name,
 * display type (Dropdown/Button/Swatch), description, ordering, and an inline
 * add/remove/reorder value list. Swatch attributes show a colour picker per value.
 */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { attributeDisplayTypeOptions } from 'modules/catalog/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))
const isSwatch = computed(() => form.displayType === 'Swatch')

let keySeq = 0
const nextKey = () => `v${keySeq++}`

const form = reactive({
  name: '',
  displayType: 'Dropdown',
  description: '',
  displayOrder: 0,
  values: []
})

const rules = { name: { required, maxLength: maxLength(200) } }
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    form.name = item?.name || ''
    form.displayType = item?.displayType || 'Dropdown'
    form.description = item?.description || ''
    form.displayOrder = item?.displayOrder ?? 0
    form.values = (item?.values || []).map((v) => ({
      _key: nextKey(),
      id: v.id || null,
      value: v.value || '',
      colorHex: v.colorHex || null
    }))
    v$.value.$reset()
  },
  { immediate: true }
)

function addValue () {
  form.values.push({ _key: nextKey(), id: null, value: '', colorHex: isSwatch.value ? '#000000' : null })
}
function removeValue (i) {
  form.values.splice(i, 1)
}
function move (i, dir) {
  const j = i + dir
  if (j < 0 || j >= form.values.length) return
  const [row] = form.values.splice(i, 1)
  form.values.splice(j, 0, row)
}

function toNumberOrZero (value) {
  const n = Number(value)
  return Number.isFinite(n) ? n : 0
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  const values = form.values
    .filter((v) => (v.value || '').trim())
    .map((v, index) => ({
      id: v.id || undefined,
      value: v.value.trim(),
      displayOrder: index,
      colorHex: form.displayType === 'Swatch' ? (v.colorHex || null) : null
    }))
  emit('submit', {
    name: form.name.trim(),
    displayType: form.displayType,
    description: form.description || null,
    displayOrder: toNumberOrZero(form.displayOrder),
    values
  })
}
</script>

<style scoped lang="scss">
.attr-swatch {
  width: 34px;
  height: 34px;
  border: 1px solid var(--sf-border, #ddd);
  border-radius: 4px;
  padding: 0;
  cursor: pointer;
  background: none;
}
</style>
