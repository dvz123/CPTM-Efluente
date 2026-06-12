<template>
  <div class="map-card">
    <div ref="mapElement" class="map-canvas"></div>

    <div class="map-toolbar">
      <div class="map-coords">
        <p class="map-label">GPS</p>
        <p class="map-value">
          <strong>{{ latitude || '—' }}</strong>,
          <strong>{{ longitude || '—' }}</strong>
        </p>
        <p class="map-hint">Toque no mapa para ajustar o ponto manualmente.</p>
        <p v-if="feedback.texto" class="map-feedback" :class="`map-feedback--${feedback.tipo}`">{{ feedback.texto }}</p>
      </div>

      <button type="button" class="map-action" @click="capturarCoordenadas">
        Usar localização atual
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import Map from '@arcgis/core/Map.js'
import MapView from '@arcgis/core/views/MapView.js'
import Graphic from '@arcgis/core/Graphic.js'
import Point from '@arcgis/core/geometry/Point.js'
import SimpleMarkerSymbol from '@arcgis/core/symbols/SimpleMarkerSymbol.js'

const props = defineProps({
  latitude: { type: [String, Number, null], default: null },
  longitude: { type: [String, Number, null], default: null }
})

const emit = defineEmits(['update:latitude', 'update:longitude'])

const mapElement = ref(null)
const feedback = ref({ tipo: 'info', texto: '' })

let view = null
let graphic = null

const parsedLatitude = computed(() => {
  const value = Number(props.latitude)
  return Number.isFinite(value) ? value : null
})

const parsedLongitude = computed(() => {
  const value = Number(props.longitude)
  return Number.isFinite(value) ? value : null
})

const defaultCenter = [-46.6388, -23.5489]
const defaultZoom = 11
const pinZoom = 16

function criarMarcador(lat, lng) {
  const point = new Point({
    longitude: lng,
    latitude: lat
  })

  const symbol = new SimpleMarkerSymbol({
    style: 'circle',
    color: '#d92d20',
    size: '14px',
    outline: {
      color: '#ffffff',
      width: 2
    }
  })

  return new Graphic({
    geometry: point,
    symbol
  })
}

function definirCoordenadas(lat, lng, center = true) {
  emit('update:latitude', lat.toFixed(6))
  emit('update:longitude', lng.toFixed(6))
  feedback.value = { tipo: 'sucesso', texto: 'Coordenadas atualizadas no formulário.' }

  if (!view) {
    return
  }

  if (graphic) {
    view.graphics.remove(graphic)
  }

  graphic = criarMarcador(lat, lng)
  view.graphics.add(graphic)

  if (center) {
    view.goTo({ center: [lng, lat], zoom: Math.max(view.zoom, pinZoom) })
  }
}

function sincronizarMarcador() {
  if (!view) {
    return
  }

  if (parsedLatitude.value === null || parsedLongitude.value === null) {
    if (graphic) {
      view.graphics.remove(graphic)
      graphic = null
    }
    return
  }

  definirCoordenadas(parsedLatitude.value, parsedLongitude.value, false)
}

function reajustarMapa(center = false) {
  if (!view) {
    return
  }

  nextTick(() => {
    window.requestAnimationFrame(() => {
      if (parsedLatitude.value !== null && parsedLongitude.value !== null) {
        definirCoordenadas(parsedLatitude.value, parsedLongitude.value, center)
        return
      }

      if (center) {
        view.goTo({ center: defaultCenter, zoom: defaultZoom })
      }
    })
  })
}

function capturarCoordenadas() {
  if (!navigator.geolocation) {
    feedback.value = { tipo: 'erro', texto: 'Geolocalização não suportada neste dispositivo.' }
    return
  }

  navigator.geolocation.getCurrentPosition(
    ({ coords }) => {
      definirCoordenadas(coords.latitude, coords.longitude)
    },
    (err) => {
      feedback.value = { tipo: 'erro', texto: `Erro ao capturar GPS: ${err.message}` }
    },
    { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
  )
}

onMounted(() => {
  const hasCoords = parsedLatitude.value !== null && parsedLongitude.value !== null
  const initialCenter = hasCoords
    ? [parsedLongitude.value, parsedLatitude.value]
    : defaultCenter

  const map = new Map({
    basemap: 'satellite'
  })

  view = new MapView({
    container: mapElement.value,
    map: map,
    center: initialCenter,
    zoom: hasCoords ? pinZoom : defaultZoom,
    ui: {
      components: ['zoom']
    }
  })

  view.when(() => {
    view.on('click', (event) => {
      const { latitude, longitude } = event.mapPoint
      definirCoordenadas(latitude, longitude, true)
    })

    sincronizarMarcador()
    reajustarMapa(true)
  })

  window.addEventListener('resize', reajustarMapa)
})

watch(() => [props.latitude, props.longitude], () => {
  sincronizarMarcador()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', reajustarMapa)

  if (graphic) {
    view?.graphics?.remove(graphic)
    graphic = null
  }

  if (view) {
    view.destroy()
    view = null
  }
})
</script>

<style scoped>
.map-card {
  position: relative;
  isolation: isolate;
  border: 1px solid var(--cptm-border);
  border-radius: 1rem;
  overflow: hidden;
  background: var(--cptm-surface);
}

.map-canvas {
  height: 19rem;
  width: 100%;
}

.map-toolbar {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: center;
  padding: 0.9rem 1rem 1rem;
  background: var(--cptm-surface);
}

.map-coords {
  min-width: 0;
}

.map-label {
  margin: 0;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--cptm-text);
}

.map-value {
  margin: 0.2rem 0 0;
  font-size: 0.82rem;
  color: var(--cptm-text);
}

.map-hint {
  margin: 0.28rem 0 0;
  font-size: 0.72rem;
  color: var(--cptm-text-muted);
}

.map-feedback {
  margin: 0.45rem 0 0;
  font-size: 0.76rem;
  line-height: 1.45;
}

.map-feedback--sucesso {
  color: #15803d;
}

.map-feedback--erro {
  color: #b91c1c;
}

.map-action {
  border: none;
  border-radius: 0.8rem;
  background: var(--cptm-primary);
  color: #fff;
  padding: 0.85rem 1rem;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
}

.map-action:hover {
  background: var(--cptm-primary-hover);
}

:deep(.esri-ui-bottom-right) {
  z-index: 1;
}

:deep(.esri-view-surface::after) {
  outline: none !important;
}

@media (max-width: 640px) {
  .map-canvas {
    height: 13.5rem;
  }

  .map-toolbar {
    flex-direction: column;
    align-items: stretch;
    padding: 0.85rem;
  }

  .map-action {
    width: 100%;
  }
}
</style>
