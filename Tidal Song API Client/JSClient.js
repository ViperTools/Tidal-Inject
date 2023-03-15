function getSongData() {
    const footer = document.getElementById('footerPlayer')
    const imageUrl = footer.querySelector('[data-test=current-media-imagery] img').src
    
    const titleLink = footer.querySelector('[data-test=footer-track-title] a')
    const trackId = titleLink.href.split('/').pop()
    const title = titleLink.textContent

    // Artists
    const artistChildren = footer.querySelector('.artist-link').children
    const artists = []

    for (const artist of artistChildren) {
        artists.push(artist.textContent)
    }

    return {
        imageUrl,
        trackId,
        title,
        artists,
        trackUrl: 'https://tidal.com/browse/track/' + trackId,
        artistsString: artists.join(', ')
    }
}

function sendState(state) {
    const xhr = new XMLHttpRequest()
    xhr.open('POST', 'C#_API_URL/status')
    xhr.setRequestHeader('Content-Type', 'application/json')
    xhr.setRequestHeader('Authorization', 'C#_AUTHORIZATION_TOKEN')
    xhr.send(JSON.stringify({
        state: state,
        songData: getSongData()
    }))
}

function onSongPlayed() {
    sendState('play')
}

function onSongPaused() {
    sendState('pause')
}

function onNewSongPlayed() {}
function onSongStopped() {}
function onSongCompleted() {}

const player = NativePlayerComponent.Player()
let { trackId } = getSongData()

player.addEventListener('mediastate', e => {
    const state = e.target

    switch (state) {
        case 'active': {
            let newTrackId = getSongData().trackId

            if (trackId != newTrackId) {
                onNewSongPlayed()
                trackId = newTrackId
            }

            setTimeout(onSongPlayed, 500)
            break;
        }
        case 'paused': onSongPaused(); break;
        case 'stopped': onSongStopped(); break;
        case 'completed': onSongCompleted(); break;
    }
})

// Send initial status
onSongPaused()