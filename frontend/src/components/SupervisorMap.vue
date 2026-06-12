<template>
  <div ref="mapElement" class="supervisor-map"></div>
</template>

<script setup>
import { onMounted, onBeforeUnmount, ref, watch } from 'vue'
import Map from '@arcgis/core/Map.js'
import MapView from '@arcgis/core/views/MapView.js'
import Graphic from '@arcgis/core/Graphic.js'
import Point from '@arcgis/core/geometry/Point.js'
import SimpleMarkerSymbol from '@arcgis/core/symbols/SimpleMarkerSymbol.js'
import PopupTemplate from '@arcgis/core/PopupTemplate.js'

const props = defineProps({
  registros: { type: Array, default: () => [] },
  centro: { type: Array, default: () => [-46.6388, -23.5489] },
  zoom: { type: Number, default: 15 }
})

const emit = defineEmits(['marcadorClick'])

const mapElement = ref(null)
let view = null

const coresTipo = {
  efluente: '#3b82f6',
  arvore: '#22c55e',
  fauna: '#f59e0b',
  erosao: '#f97316',
  residuo: '#a855f7',
  default: '#64748b'
}

function criarMarcador(reg) {
  const lat = reg.latitude
  const lng = reg.longitude
  if (lat == null || lng == null) return null

  const tipo = (reg.natureza || '').toLowerCase()
  const cor = coresTipo[tipo] || coresTipo.default

  const point = new Point({ longitude: lng, latitude: lat })

  const symbol = new SimpleMarkerSymbol({
    style: 'circle',
    color: cor,
    size: '12px',
    outline: { color: '#ffffff', width: 2 }
  })

  const popupTemplate = new PopupTemplate({
    title: reg.natureza || 'Registro',
    content: `
      <div style="font-size:13px;line-height:1.5">
        <p><strong>ID:</strong> ${reg.pk || '-'}</p>
        <p><strong>Operador:</strong> ${reg.autor || '-'}</p>
        <p><strong>Estação:</strong> ${reg.estacao || '-'}</p>
        <p><strong>Data:</strong> ${reg.dataCadastro ? new Date(reg.dataCadastro).toLocaleString('pt-BR') : '-'}</p>
        <p><strong>Coords:</strong> ${lat.toFixed(5)}, ${lng.toFixed(5)}</p>
      </div>
    `
  })

  return new Graphic({
    geometry: point,
    symbol,
    attributes: { ...reg },
    popupTemplate
  })
}

function renderizarMarcadores() {
  if (!view) return
  view.graphics.removeAll()

  const graphics = props.registros
    .map(criarMarcador)
    .filter(Boolean)

  if (graphics.length) {
    view.graphics.addMany(graphics)
  }
}

onMounted(() => {
  const map = new Map({ basemap: 'satellite' })



  view = new MapView({
    container: mapElement.value,
    map,
    center: props.centro,
    zoom: props.zoom,
    ui: { components: [] }
  })



  view.when(() => {
    renderizarMarcadores()

    view.on('click', (event) => {
      view.hitTest(event).then((response) => {
        const graphic = response.results.find(r => r.graphic?.attributes?.pk)
        if (graphic) {
          emit('marcadorClick', graphic.graphic.attributes)
        }
      })
    })
  })
})

watch(() => props.registros, () => {
  renderizarMarcadores()
}, { deep: true })


watch(() => [props.centro, props.zoom], ([newCentro, newZoom]) => {
  if (view) {
    view.goTo({ center: newCentro, zoom: newZoom })
  }
})

onBeforeUnmount(() => {

  if (view) {
    view.destroy()
    view = null
  }
})
</script>

<style scoped>
.supervisor-map {
  width: 100%;
  height: 100%;
  min-height: 0;
}

:deep(.esri-view-surface::after) {
  outline: none !important;
}

:deep(.esri-ui-corner) {
  z-index: 10;
}
</style>
