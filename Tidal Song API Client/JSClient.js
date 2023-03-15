const state = { paused: true, trackId: null }
const elements = {}

function getElements() {
    const footer = document.getElementById('footerPlayer')

    if (footer) {
        elements.image = footer.querySelector('[data-test=current-media-imagery] img')
        elements.title = footer.querySelector('[data-test=footer-track-title] a')
        elements.artist = footer.querySelector('.artist-link')
        elements.playButton = footer.querySelector('div[class^=playbackButton] button')
    }

    return !Object.values(elements).includes(null)
}

function getSongData() {
    const { image, title, artist } = elements
    const artists = Array.from(artist.children, e => e.innerText)
    const trackId = title.href.split('/').pop()

    return {
        artists,
        trackId,
        imageUrl: image.src,
        title: title.textContent,
        trackUrl: 'https://tidal.com/browse/track/' + trackId,
        artistsString: artists.join(', ')
    }
}

function sendState(end) {
    const xhr = new XMLHttpRequest()
    xhr.open('POST', 'C#_API_URL/status')
    xhr.setRequestHeader('Content-Type', 'application/json')
    xhr.setRequestHeader('Authorization', 'C#_AUTHORIZATION_TOKEN')

    if (!end) {
        xhr.send(JSON.stringify({
            state: state.paused ? 'pause' : 'play',
            songData: getSongData()
        }))
    }
    else {
        xhr.send('null')
    }
}

// Loop check status (used to use NativePlayerComponent but it doesn't seem to work on MacOS)
function update() {
    // Wait for stuff to load
    if (!getElements()) {
        setTimeout(update, 500)
        return;
    }

    // Handle status
    const { trackId } = getSongData()
    const paused = elements.playButton.getAttribute('aria-label') == 'Play'

    if (paused != state.paused || trackId != state.trackId) {
        state.paused = paused
        state.trackId = trackId
        sendState()
    }

    setTimeout(update, 100)
}

update()

window.addEventListener('beforeunload', () => {
    sendState(true)
});