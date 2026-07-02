<template>
  <div v-if="variants.length" class="variant-selector">
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
        @click="pick(v)"
      >
        {{ variantLabel(v) }}
        <q-tooltip v-if="!v.isEnabled">Unavailable</q-tooltip>
      </q-chip>
    </div>
  </div>
</template>

<script setup>
/*
 * Variant selector (WO-19): renders the product's variants as selectable chips
 * and emits the chosen variant id (or null when deselected). The storefront
 * detail DTO exposes only a variant's SKU, price, stock and attribute value ids
 * (no attribute value text), so each chip is labelled by SKU with a price hint
 * rather than grouped attribute pickers. Clicking the active chip clears the
 * selection (falls back to the base product price / media).
 */
import { formatPrice } from 'modules/storefront/api'

const props = defineProps({
  // StorefrontVariantDto[]: { id, sku, price, stockQuantity, isEnabled, attributeValueIds }
  variants: { type: Array, default: () => [] },
  modelValue: { type: [String, Number, null], default: null }
})

const emit = defineEmits(['update:modelValue'])

function variantLabel (v) {
  const name = v.sku || ('Variant ' + String(v.id).slice(0, 8))
  return v.price != null ? name + ' · ' + formatPrice(v.price) : name
}

function pick (v) {
  if (!v.isEnabled) return
  emit('update:modelValue', props.modelValue === v.id ? null : v.id)
}
</script>
