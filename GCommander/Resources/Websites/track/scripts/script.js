const map = L.map('map').setView([51.505, -0.09], 13)
const tiles = L.tileLayer('https://{s}.tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OSM</a>'
}).addTo(map)
const polyline = L.polyline([], { fillColor: 'red', color: 'blue' }).addTo(map)

function setTrack(trk) {
    const latLngs = [trk.trackPoints.map(n => [n.latitude, n.longitude])]
    polyline.setLatLngs(latLngs)
    map.fitBounds(polyline.getBounds())
}