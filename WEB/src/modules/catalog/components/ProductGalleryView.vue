<template>
  <div class="product-gallery">
    <q-carousel
      v-if="displayImages.length"
      v-model="slide"
      class="product-gallery__main rounded-borders bg-grey-1"
      swipeable
      animated
      :arrows="displayImages.length > 1"
      :navigation="false"
      control-color="primary"
      height="420px"
    >
      <q-carousel-slide
        v-for="img in displayImages"
        :key="img.id"
        :name="img.id"
        class="column no-wrap flex-center q-pa-none"
      >
        <!-- Video embed entry -->
        <q-video
          v-if="img.mediaType === 'Video' || img.mediaType === 1"
          :ratio="16 / 9"
          :src="img.url"
          class="product-gallery__video"
        />
        <!-- Image entry: QImg lazy-loads via IntersectionObserver -->
        <q-img
          v-else
          :src="img.url"
          :alt="img.altText || productName"
          fit="contain"
          loading="lazy"
          class="product-gallery__image fit"
          no-spinner
        />
      </q-carousel-slide>
    </q-carousel>

    <div
      v-else
      class="product-gallery__main product-gallery__empty rounded-borders bg-grey-1 column flex-center text-grey-6"
    >
      <q-icon name="o_image" size="64px" />
      <div class="q-mt-sm">No images</div>
    </div>

    <!-- Thumbnail strip -->
    <div v-if="displayImages.length > 1" class="product-gallery__thumbs row no-wrap q-gutter-sm q-mt-sm">
      <div
        v-for="img in displayImages"
        :key="`thumb-${img.id}`"
        class="product-gallery__thumb cursor-pointer rounded-borders"
        :class="{ 'product-gallery__thumb--active': img.id === slide }"
        @click="select(img.id)"
      >
        <q-img
          :src="img.thumbnailUrl || img.url"
          :alt="img.altText || productName"
          fit="cover"
          loading="lazy"
          width="64px"
          height="64px"
          class="rounded-borders"
        >
          <q-icon
            v-if="img.mediaType === 'Video' || img.mediaType === 1"
            name="o_play_circle"
            size="24px"
            class="absolute-center text-white"
          />
        </q-img>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  // Full media set for the product. Each: { id, url, thumbnailUrl, altText, mediaType, displayOrder, productVariantId }
  images: { type: Array, default: () => [] },
  // Currently selected variant id; when set, the gallery shows that variant's media (AC-CAT-PZG-001.3).
  variantId: { type: [String, Number, null], default: null },
  productName: { type: String, default: '' }
})

const slide = ref(null)

// Images shown: variant-specific media when a variant is selected and has its own images,
// otherwise fall back to product-level media (productVariantId == null). Sorted by displayOrder.
const displayImages = computed(() => {
  const all = [...(props.images || [])].sort(
    (a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0)
  )
  if (props.variantId != null) {
    const variantImages = all.filter((i) => i.productVariantId === props.variantId)
    if (variantImages.length) return variantImages
  }
  const productLevel = all.filter((i) => i.productVariantId == null)
  return productLevel.length ? productLevel : all
})

function select (id) {
  slide.value = id
}

// Reset to the first image whenever the visible set changes (variant switch or data load).
watch(
  displayImages,
  (imgs) => {
    if (!imgs.length) {
      slide.value = null
      return
    }
    if (!imgs.some((i) => i.id === slide.value)) {
      slide.value = imgs[0].id
    }
  },
  { immediate: true }
)
</script>

<style scoped lang="scss">
.product-gallery__main {
  width: 100%;
}

.product-gallery__empty {
  height: 420px;
}

.product-gallery__thumbs {
  overflow-x: auto;
  padding-bottom: 4px;
}

.product-gallery__thumb {
  border: 2px solid transparent;
  transition: border-color 0.2s ease;
  flex: 0 0 auto;

  &--active {
    border-color: var(--q-primary);
  }
}
</style>
