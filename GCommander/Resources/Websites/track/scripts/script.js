const map = L.map('map').setView([51.505, -0.09], 13)

const tiles = L.tileLayer('https://{s}.tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OSM</a>'
}).addTo(map)

function setTrack(trk) {
    //alert(trk.trackPoints[0].latitude)
    map.setView([trk.trackPoints[0].latitude, trk.trackPoints[0].longitude], 13)
}