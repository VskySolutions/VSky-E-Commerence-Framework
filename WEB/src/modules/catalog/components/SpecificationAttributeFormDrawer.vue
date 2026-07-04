<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit specification attribute' : 'New specification attribute'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" placeholder="e.g. Material, Warranty" />

    <div class="row items-center q-col-gutter-sm q-mt-xs">
      <div class="col-6">
        <AppTextField v-model="form.displayOrder" label="Display order" type="number" />
      </div>
      <div class="col-6">
        <q-toggle v-model="form.isFilterable" label="Filterable" color="primary" />
        <div class="text-caption text-grey-6">Show in storefront filters</div>
      </div>
    </div>

    <q-separator class="q-my-md" />

    <div class="row items-center justify-between q-mb-sm">
      <AppFieldLabel label="Options" />
      <q-btn flat dense no-caps color="primary" icon="o_add" label="Add option" @click="addOption" />
    </div>

    <div v-if="!form.options.length" class="text-grey-6 text-caption q-mb-sm">
      No options yet. Add one or more (e.g. Cotton, Leather).
    </div>

    <div v-for="(opt, i) in form.options" :key="opt._key" class="row items-center q-col-gutter-xs q-mb-xs no-wrap">
      <div class="col-auto column">
        <q-btn flat dense round size="sm" icon="keyboard_arrow_up" :disable="i === 0" @click="move(i, -1)" />
        <q-btn flat dense round size="sm" icon="keyboard_arrow_down" :disable="i === form.options.length - 1" @click="move(i, 1)" />
      </div>
      <div class="col">
        <q-input v-model="opt.value" dense outlined placeholder="Option label" />
      </div>
      <div class="col-auto">
        <q-btn flat dense round size="sm" icon="o_delete" color="negative" @click="removeOption(i)" />
      </div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * SpecificationAttributeFormDrawer (WO-15): create/edit a global specification
 * attribute — name, a "Filterable" toggle (drives storefront faceted navigation),
 * ordering, and an inline add/remove/reorder option list. No display type/colour
 * (spec attributes don't drive variant selection).
 */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

let keySeq = 0
const nextKey = () => `o${keySeq++}`

const form = reactive({
  name: '',
  isFilterable: true,
  displayOrder: 0,
  options: []
})

const rules = { name: { required, maxLength: maxLength(200) } }
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    form.name = item?.name || ''
    form.isFilterable = item?.isFilterable ?? true
    form.displayOrder = item?.displayOrder ?? 0
    form.options = (item?.options || []).map((o) => ({
      _key: nextKey(),
      id: o.id || null,
      value: o.value || ''
    }))
    v$.value.$reset()
  },
  { immediate: true }
)

function addOption () {
  form.options.push({ _key: nextKey(), id: null, value: '' })
}
function removeOption (i) {
  form.options.splice(i, 1)
}
function move (i, dir) {
  const j = i + dir
  if (j < 0 || j >= form.options.length) return
  const [row] = form.options.splice(i, 1)
  form.options.splice(j, 0, row)
}

function toNumberOrZero (value) {
  const n = Number(value)
  return Number.isFinite(n) ? n : 0
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  const options = form.options
    .filter((o) => (o.value || '').trim())
    .map((o, index) => ({ id: o.id || undefined, value: o.value.trim(), displayOrder: index }))
  emit('submit', {
    name: form.name.trim(),
    isFilterable: form.isFilterable,
    displayOrder: toNumberOrZero(form.displayOrder),
    options
  })
}
</script>
