const map = L.map('map').setView([51.505, -0.09], 13)
const tiles = L.tileLayer('https://{s}.tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OSM</a>'
}).addTo(map)
const polyline = L.polyline([], { fillColor: 'red', color: 'blue' }).addTo(map)
const marker = L.marker([50, 9]).addTo(map)
const range = document.getElementById("range")
range.addEventListener("input", _ => {
    console.log("änderung, ", range.value, factor)
    marker.setLatLng(latLngs[Math.floor(range.value * factor)])
})

function setTrack(trk) {
    factor = trk.trackPoints.length / 1000
    latLngs = trk.trackPoints.map(n => [n.latitude, n.longitude])

    polyline.setLatLngs(latLngs)
    map.fitBounds(polyline.getBounds())
    range.value = 0
    marker.setLatLng(latLngs[0])
}

var factor
var latLngs