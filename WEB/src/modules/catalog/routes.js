/* Catalog module routes (WO-15): products (+ detail), categories, manufacturers. */
export default [
  {
    path: 'catalog/products',
    name: 'catalog-products',
    meta: { title: 'Products', permissions: ['Catalog.Read'] },
    component: () => import('modules/catalog/pages/products.vue')
  },
  {
    path: 'catalog/products/:id',
    name: 'catalog-product-detail',
    meta: { title: 'Product', permissions: ['Catalog.Read'] },
    component: () => import('modules/catalog/pages/product-detail.vue')
  },
  {
    path: 'catalog/categories',
    name: 'catalog-categories',
    meta: { title: 'Categories', permissions: ['Catalog.Read'] },
    component: () => import('modules/catalog/pages/categories.vue')
  },
  {
    path: 'catalog/manufacturers',
    name: 'catalog-manufacturers',
    meta: { title: 'Manufacturers', permissions: ['Catalog.Read'] },
    component: () => import('modules/catalog/pages/manufacturers.vue')
  }
]
