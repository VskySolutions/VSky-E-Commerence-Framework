<template>
  <div class="app-field app-richtext">
    <AppFieldLabel v-if="label" :label="label" :required="required">
      <template v-if="$slots.hint || hint" #hint>
        <slot name="hint">{{ hint }}</slot>
      </template>
    </AppFieldLabel>

    <div class="app-richtext__wrap" :class="{ 'app-richtext__wrap--error': hasError }">
      <q-editor
        :model-value="modelValue || ''"
        :min-height="minHeight"
        :toolbar="toolbar"
        :definitions="definitions"
        flat
        content-class="app-richtext__content"
        @update:model-value="onInput"
        @blur="onBlur"
      />
      <div v-if="showPlaceholder" class="app-richtext__placeholder">{{ placeholder }}</div>
    </div>

    <div v-if="errorMessage" class="app-richtext__error">{{ errorMessage }}</div>
  </div>
</template>

<script setup>
/*
 * AppRichText: the portal-standard rich text editor (WYSIWYG) that replaces plain
 * "description" textareas across the app. Wraps q-editor with an AppFieldLabel
 * (label + hint), an empty-state placeholder, and Vuelidate error display (pass
 * :v="v$.field"). v-model is HTML.
 */
import { computed } from 'vue'
import AppFieldLabel from './AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, default: '' },
  required: { type: Boolean, default: false },
  hint: { type: String, default: '' },
  placeholder: { type: String, default: 'Write a description…' },
  minHeight: { type: String, default: '8rem' },
  v: { type: Object, default: null }
})

const emit = defineEmits(['update:modelValue'])

const toolbar = [
  ['bold', 'italic', 'underline', 'strike'],
  [{ label: 'Format', icon: 'format_size', list: 'no-icons', options: ['p', 'h2', 'h3', 'h4'] }],
  ['unordered', 'ordered'],
  ['quote', 'link'],
  ['undo', 'redo'],
  ['removeFormat']
]

const definitions = {
  link: { label: 'Link', icon: 'link' }
}

// Placeholder shows only when the editor has no meaningful content.
const showPlaceholder = computed(() => {
  const v = props.modelValue || ''
  const stripped = v.replace(/<[^>]*>/g, '').replace(/&nbsp;/g, '').trim()
  return stripped.length === 0
})

const hasError = computed(() => !!(props.v && props.v.$error))
const errorMessage = computed(() => {
  if (props.v && Array.isArray(props.v.$errors) && props.v.$errors.length) {
    return props.v.$errors[0].$message
  }
  return undefined
})

function onInput (value) {
  emit('update:modelValue', value)
}

function onBlur () {
  if (props.v && typeof props.v.$touch === 'function') props.v.$touch()
}
</script>

<style scoped lang="scss">
.app-richtext__wrap {
  position: relative;
  border: 1px solid rgba(0, 0, 0, 0.24);
  border-radius: 4px;
  overflow: hidden;
}
.app-richtext__wrap--error {
  border-color: var(--q-negative);
}
.app-richtext__placeholder {
  position: absolute;
  left: 12px;
  bottom: 10px;
  color: rgba(0, 0, 0, 0.45);
  font-size: 13px;
  pointer-events: none;
}
.app-richtext__error {
  color: var(--q-negative);
  font-size: 11px;
  margin-top: 4px;
}
:deep(.app-richtext__content) {
  min-height: 6rem;
}
</style>
