let audioCtx;//= new AudioContext();
let listener;

let outdoorLowpassNode;
let outdoorGainNode;

const audioBufferCache = {};
const sounds = {
    bear: [
        "sfx/bear-01.wav",
        "sfx/bear-02.wav",
        "sfx/bear-03.wav",
        "sfx/bear-04.wav",
        "sfx/bear-05.wav",
        "sfx/bear-06.wav",
    ],
    outdoorAmbience: "sfx/outdoors.ogg"
};

async function initaliseAudio() {
    audioCtx = new AudioContext();
    listener = audioCtx.listener;

    outdoorLowpassNode = new BiquadFilterNode(audioCtx);
    outdoorLowpassNode.type = 'lowpass';
    outdoorLowpassNode.frequency.value = 1500;
    
    outdoorGainNode = new GainNode(audioCtx);
    outdoorGainNode.gain.value = 0.5;
    outdoorLowpassNode.connect(outdoorGainNode).connect(audioCtx.destination)

    let outdoorSound = await createAmbientSpeaker('sfx/outdoors.ogg');
    outdoorSound.start();

    const clowntje = await createPointSpeaker('c.mp3', 0, 0, 0);
    clowntje.loop = true;
    clowntje.start();
}

async function loadAudio(path) {
    if (audioBufferCache[path])
        return audioBufferCache[path];
    const response = await fetch(path);
    const data = await response.arrayBuffer();
    const buffer = await audioCtx.decodeAudioData(data);
    audioBufferCache[path] = buffer;
    return buffer;
}

async function createPointSpeaker(audioPath, x, y, z, rolloffFactor = 2, maxDistance = 100) {
    const source = audioCtx.createBufferSource();
    source.buffer = await loadAudio(audioPath);
    const panner = new PannerNode(audioCtx, {
        panningModel: 'HRTF',
        distanceModel: 'inverse',
        positionX: x,
        positionY: y,
        positionZ: z,
        refDistance: 1,
        maxDistance,
        rolloffFactor,
    })
    source.connect(panner).connect(audioCtx.destination);
    return source;
}

async function createAmbientSpeaker(audioPath) {
    const source = audioCtx.createBufferSource();
    source.buffer = await loadAudio(audioPath);
    source.loop = true;
    source.connect(outdoorLowpassNode);
    return source;
}