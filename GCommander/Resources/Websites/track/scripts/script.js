const map = L.map('map').setView([51.505, -0.09], 13)
const tiles = L.tileLayer('https://{s}.tile.openstreetmap.de/tiles/osmde/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OSM</a>'
}).addTo(map)
const polyline = L.polyline([], { fillColor: 'red', color: 'blue' }).addTo(map)
const marker = L.marker([50, 9]).addTo(map)
const range = document.getElementById("range")
const distance = document.getElementById("distance")
const duration = document.getElementById("duration")
const averageSpeed = document.getElementById("averageSpeed")
const maxSpeed = document.getElementById("maxSpeed")
const averageHeartRate = document.getElementById("averageHeartRate")
const maxHeartRate = document.getElementById("maxHeartRate")
const speed = document.getElementById("speed")
const heartrate = document.getElementById("heartrate")

range.addEventListener("input", _ => {
    const pos = Math.floor(range.value * factor)
    marker.setLatLng(latLngs[pos])
    speed.innerText = `${trackPoints[pos].velocity?.toFixed(1)} km/h`
    heartrate.innerText = `❤️ ${trackPoints[pos].heartrate}`
})

function setTrack(trk) {
    factor = trk.trackPoints.length / 1000
    latLngs = trk.trackPoints.map(n => [n.latitude, n.longitude])

    polyline.setLatLngs(latLngs)
    map.fitBounds(polyline.getBounds())
    range.value = 0
    marker.setLatLng(latLngs[0])
    distance.innerText = `${trk.distance.toFixed(1)} km`
    duration.innerText = `${formatDistance(trk.duration)}`
    averageSpeed.innerText = `Ø ${trk.averageSpeed?.toFixed(1)} km/h`
    maxSpeed.innerText = `Ø ${(trk.trackPoints ? Math.max(...trk.trackPoints.map(n => n.velocity)) : 0).toFixed(1)} km/h`
    averageSpeed.innerText = `Ø ${trk.averageSpeed?.toFixed(1)} km/h`
    averageHeartRate.innerText = `Ø❤️ ${trk.averageHeartRate}` 
    maxHeartRate.innerText = `Max❤️ ${(trk.trackPoints ? Math.max(...trk.trackPoints.map(n => n.heartrate)) : 0)}` 
    trackPoints = trk.trackPoints
    speed.innerText = `${trackPoints[0].velocity?.toFixed(1)} km/h`
    heartrate.innerText = `❤️ ${trackPoints[0].heartrate}`
}

function formatDistance(dist) {
    const pad = n => ('' + n).padStart(2, '0')

    const hours = Math.floor(dist / 3600)
    const min = Math.floor(dist % 3600 / 60)
    
    return `${pad(hours)}:${pad(min)}`
}


var factor
var latLngs
var trackPoints