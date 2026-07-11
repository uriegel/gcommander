const map = L.map('map').setView([51.505, -0.09], 13)

const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
}).addTo(map)

const marker = L.marker([50, 9]).addTo(map)

function setLocation(lat, lon) {
    map.setView([lat, lon], 13)
    marker.setLatLng([lat, lon])
}
