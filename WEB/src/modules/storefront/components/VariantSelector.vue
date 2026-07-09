<template>
  <!-- Proper grouped attribute pickers (Colour swatches, Size buttons, …) -->
  <div v-if="hasAttributes" class="variant-selector column q-gutter-md">
    <div v-for="attr in attributes" :key="attr.id" class="vs-attr">
      <div class="text-body2 q-mb-xs">
        <span class="text-weight-medium">{{ attr.name }}</span>
        <span v-if="selectedValueName(attr)" class="text-grey-8">: {{ selectedValueName(attr) }}</span>
      </div>

      <!-- Swatch: colour circles -->
      <div v-if="showSwatches(attr)" class="row q-gutter-sm">
        <button
          v-for="val in attr.values"
          :key="val.id"
          type="button"
          class="vs-swatch"
          :class="{ 'vs-swatch--active': selected[attr.id] === val.id, 'vs-swatch--disabled': !isAvailable(attr, val.id) }"
          :style="{ background: resolveColor(val) || '#e0e0e0' }"
          :aria-label="val.value"
          @click="pick(attr, val.id)"
        >
          <q-icon v-if="selected[attr.id] === val.id" name="o_check" size="16px" class="vs-swatch__check" />
          <q-tooltip>{{ val.value }}<template v-if="!isAvailable(attr, val.id)"> — unavailable</template></q-tooltip>
        </button>
      </div>

      <!-- Button / Dropdown: labelled options -->
      <div v-else class="row q-gutter-sm">
        <button
          v-for="val in attr.values"
          :key="val.id"
          type="button"
          class="vs-option"
          :class="{ 'vs-option--active': selected[attr.id] === val.id, 'vs-option--disabled': !isAvailable(attr, val.id) }"
          @click="pick(attr, val.id)"
        >
          {{ val.value }}
        </button>
      </div>
    </div>
  </div>

  <!-- Fallback: no attribute metadata available — legacy SKU chips -->
  <div v-else-if="variants.length" class="variant-selector">
    <div class="text-caption text-grey-7 q-mb-xs">Options</div>
    <div class="row q-gutter-sm">
      <q-chip
        v-for="v in variants"
        :key="v.id"
        clickable
        :outline="modelValue !== v.id"
        :color="modelValue === v.id ? 'primary' : 'grey-3'"
        :text-color="modelValue === v.id ? 'white' : 'dark'"
        :disable="!v.isEnabled"
        @click="pickVariant(v)"
      >
        {{ variantLabel(v) }}
        <q-tooltip v-if="!v.isEnabled">Unavailable</q-tooltip>
      </q-chip>
    </div>
  </div>
</template>

<script setup>
/*
 * Variant selector (WO-19): renders the product's variant-driving attributes as grouped pickers —
 * colour swatches for Swatch attributes, labelled buttons for Button/Dropdown — and resolves the
 * selected value combination to a concrete variant, emitting its id (or null while incomplete).
 * Values that can't combine with the current selection into an enabled variant are greyed out.
 * Falls back to legacy SKU chips when the product has variants but no attribute metadata.
 */
import { reactive, computed, watch } from 'vue'
import { formatPrice } from 'modules/storefront/api'

const props = defineProps({
  // StorefrontAttributeDto[]: { id, name, displayType, values: [{ id, value, colorHex }] }
  attributes: { type: Array, default: () => [] },
  // StorefrontVariantDto[]: { id, sku, price, stockQuantity, isEnabled, attributeValueIds }
  variants: { type: Array, default: () => [] },
  modelValue: { type: [String, Number, null], default: null }
})

const emit = defineEmits(['update:modelValue'])

// attributeId -> selected valueId
const selected = reactive({})

const hasAttributes = computed(() =>
  props.attributes.length > 0 && props.attributes.some((a) => a.values && a.values.length)
)

function isSwatch (a) { return a.displayType === 'Swatch' || a.displayType === 2 }
// Treat any colour-named attribute as swatch-capable too, so colours render even when the admin
// left the display type as Dropdown.
function isColorAttr (a) { return isSwatch(a) || /colou?r/i.test(a.name || '') }

// A value's swatch colour: an explicit hex, else the value's own name when it's a valid CSS colour
// (e.g. "Red" -> red, "Light Blue" -> lightblue). Returns null when nothing resolves.
const _probe = typeof document !== 'undefined' ? document.createElement('span') : null
function cssNamedColor (name) {
  if (!name || !_probe) return null
  const candidate = String(name).trim().toLowerCase().replace(/\s+/g, '')
  if (!candidate) return null
  _probe.style.color = ''
  _probe.style.color = candidate
  return _probe.style.color ? candidate : null
}
function resolveColor (val) { return val.colorHex || cssNamedColor(val.value) }

// Show swatches only for a colour attribute whose every value resolves to a colour; otherwise the
// attribute renders as labelled buttons.
function showSwatches (attr) {
  return isColorAttr(attr) && (attr.values || []).length > 0 && attr.values.every((v) => !!resolveColor(v))
}

function variantIds (v) { return v.attributeValueIds || [] }

// The variant defined by the current (complete) selection, if any enabled one matches.
const matched = computed(() => {
  const ids = props.attributes.map((a) => selected[a.id]).filter(Boolean)
  if (!ids.length || ids.length !== props.attributes.length) return null
  return props.variants.find((v) =>
    v.isEnabled && variantIds(v).length === ids.length && ids.every((id) => variantIds(v).includes(id))
  ) || null
})

// A value is available if some enabled variant carries it together with the OTHER current selections.
function isAvailable (attr, valueId) {
  const others = props.attributes
    .filter((a) => a.id !== attr.id)
    .map((a) => selected[a.id])
    .filter(Boolean)
  return props.variants.some((v) =>
    v.isEnabled && variantIds(v).includes(valueId) && others.every((id) => variantIds(v).includes(id))
  )
}

function selectedValueName (attr) {
  const val = (attr.values || []).find((v) => v.id === selected[attr.id])
  return val ? val.value : ''
}

function pick (attr, valueId) {
  if (!isAvailable(attr, valueId)) return
  selected[attr.id] = selected[attr.id] === valueId ? null : valueId
  emit('update:modelValue', matched.value ? matched.value.id : null)
}

// ---- Legacy fallback (no attribute metadata) ----
function variantLabel (v) {
  const name = v.sku || ('Variant ' + String(v.id).slice(0, 8))
  return v.price != null ? name + ' · ' + formatPrice(v.price) : name
}
function pickVariant (v) {
  if (!v.isEnabled) return
  emit('update:modelValue', props.modelValue === v.id ? null : v.id)
}

// Sync local selection when the parent sets/clears the variant externally (e.g. on product load).
function selectFromVariant (variant) {
  if (!variant) return
  for (const attr of props.attributes) {
    const val = (attr.values || []).find((v) => variantIds(variant).includes(v.id))
    selected[attr.id] = val ? val.id : null
  }
}

watch(
  () => props.modelValue,
  (id) => {
    if (!id) {
      for (const k of Object.keys(selected)) selected[k] = null
      return
    }
    if (matched.value && matched.value.id === id) return
    selectFromVariant(props.variants.find((v) => v.id === id))
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.vs-swatch {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  border: 2px solid var(--sf-border);
  padding: 0;
  cursor: pointer;
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: box-shadow 0.2s ease, border-color 0.2s ease, transform 0.2s ease;
}
.vs-swatch:hover { transform: scale(1.08); }
.vs-swatch--active {
  border-color: var(--sf-accent);
  box-shadow: 0 0 0 2px var(--sf-accent);
}
.vs-swatch--disabled {
  opacity: 0.35;
  cursor: not-allowed;
}
.vs-swatch--disabled:hover { transform: none; }
.vs-swatch__check {
  color: #fff;
  filter: drop-shadow(0 0 1px rgba(0, 0, 0, 0.7));
}

.vs-option {
  padding: 7px 16px;
  border-radius: var(--sf-radius);
  border: 1px solid var(--sf-border);
  background: #fff;
  color: var(--sf-heading);
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, border-color 0.2s ease, color 0.2s ease;
}
.vs-option:hover { border-color: var(--sf-accent); color: var(--sf-accent); }
.vs-option--active,
.vs-option--active:hover {
  background: var(--sf-accent);
  border-color: var(--sf-accent);
  color: #fff;
}
.vs-option--disabled,
.vs-option--disabled:hover {
  opacity: 0.4;
  cursor: not-allowed;
  text-decoration: line-through;
  border-color: var(--sf-border);
  color: var(--sf-heading);
  background: #fff;
}
</style>
