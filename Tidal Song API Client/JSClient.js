const state = { paused: true, trackId: null }
const elements = {}
const textEncoder = new TextEncoder()
let serialPort

function getElements() {
    const footer = document.getElementById('footerPlayer')

    if (footer) {
        elements.image = footer.querySelector('[data-test=current-media-imagery] img')
        elements.title = footer.querySelector('[data-test=footer-track-title] a')
        elements.artist = footer.querySelector('.artist-link')
        elements.playButton = footer.querySelector('div[class^=playbackButton] button')
        elements.nextButton = footer.querySelector('[data-test=next]')
        elements.previousButton = footer.querySelector('[data-test=previous]')
    }

    return footer && !Object.values(elements).includes(null)
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
        const songData = getSongData()

        xhr.send(JSON.stringify({
            state: state.paused ? 'pause' : 'play',
            songData
        }))

        if (serialPort) {
            const writer = serialPort.writable.getWriter()
            writer.write(textEncoder.encode(`${songData.title}\n${songData.artistsString}\n`))
            writer.releaseLock()
        }
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

// Arduino Integration

const player = NativePlayerComponent.Player()

function handleCommand(data) {
    const [ command, value ] = data.split(' ')

    switch (command.trim()) {
        case 'SET_VOLUME': {
            player.setVolume(Number(value))

            break
        }
        case 'PAUSE_SONG': {
            if (state.paused) {
                player.play()
            }
            else {
                player.pause()
            }

            break
        }
        case 'NEXT_SONG': {
            elements.nextButton.click()

            break
        }
        case 'PREVIOUS_SONG': {
            elements.previousButton.click()

            break
        }
    }
}

var serialData = ''

navigator.serial.requestPort({ filters: [{ usbVendorId: 0x2341 }] }).then(async port => {
    await port.open({ baudRate: 115200 })
    serialPort = port

    while (port.readable) {
        const reader = port.readable.getReader();

        while (true) {
            const { value, done } = await reader.read();

            if (done) {
                reader.releaseLock();
                break;
            }

            if (value) {
                serialData += String.fromCharCode(...value)

                if (serialData.endsWith('\n')) {
                    handleCommand(serialData.substring(0, serialData.length - 1))
                    serialData = ''
                }
            }
        }
    }
}).catch(console.log)