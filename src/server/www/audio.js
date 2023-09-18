let audioCtx;//= new AudioContext();
let listener;

let outdoorLowpassNode;
let outdoorGainNode;

let crowdSource;
let crowdGainNode;

let gramophoneSource;
let gramophoneLowpassNode;
let gramophoneAudioElem;

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
    creaking: [
        "sfx/wood-creak-01.wav",
        "sfx/wood-creak-02.wav",
        "sfx/wood-creak-03.wav",
    ],
    gladiators: [
        "sfx/circus-01.ogg",
        "sfx/circus-02.ogg",
    ],
    cloth: [
        "sfx/cloth-01.wav",
        "sfx/cloth-02.wav",
        "sfx/cloth-03.wav",
    ],
    outdoorAmbience: "sfx/night.ogg",
    peopleTalking: "sfx/people-talking.ogg",
    wilJeNietHoren: "sfx/ditwiljenooithoren.mp3",
};

async function initaliseAudio() {
    audioCtx = new AudioContext();
    listener = audioCtx.listener;
    gramophoneAudioElem = document.createElement('audio');

    outdoorLowpassNode = new BiquadFilterNode(audioCtx);
    outdoorLowpassNode.type = 'lowpass';
    outdoorLowpassNode.frequency.value = 1500;

    outdoorGainNode = new GainNode(audioCtx);
    outdoorGainNode.gain.value = 0.5;
    outdoorLowpassNode.connect(outdoorGainNode).connect(audioCtx.destination)

    let outdoorSound = await createAmbientSpeaker(sounds.outdoorAmbience);
    outdoorSound.start();

    const lamps = await createPointSpeaker('sfx/lamp.wav', 0, 9, 0, 2, 30);
    lamps.loop = true;
    lamps.start();


    const lamp1 = await createPointSpeaker('sfx/lamp.wav', 5.8, 0, 16.6, 7, 5);
    lamp1.loop = true;
    lamp1.start();

    const lamp2 = await createPointSpeaker('sfx/lamp.wav', -5.8, 0, 16.6, 7, 5);
    lamp2.loop = true;
    lamp2.start();

    gramophoneAudioElem.loop = true;

    gramophoneSource = audioCtx.createMediaElementSource(gramophoneAudioElem)
    const gramophoneReverb = new ConvolverNode(audioCtx, {
        buffer: await loadAudio("sfx/impulse.wav")
    });
    gramophoneLowpassNode = new BiquadFilterNode(audioCtx);
    gramophoneLowpassNode.type = 'lowpass';
    gramophoneLowpassNode.frequency.value = 1500;
    const gramophoneSourcePanner = setAudioPosition(gramophoneSource, -4.53, 1.5, 5.422, 2, 5);
    gramophoneSourcePanner.connect(gramophoneLowpassNode).connect(gramophoneReverb).connect(audioCtx.destination);
    // gramophoneAudioElem.play();
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

async function createPointSpeaker(audioPath, x, y, z, rolloffFactor = 2, maxDistance = 100, autoConnect = true) {
    const source = audioCtx.createBufferSource();
    source.buffer = await loadAudio(audioPath);
    const panner = setAudioPosition(source, x, y, z, rolloffFactor, maxDistance);
    if (autoConnect)
        panner.connect(audioCtx.destination);

    return source;
}

function setAudioPosition(source, x, y, z, rolloffFactor = 2, maxDistance = 100) {
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
    source.connect(panner);
    return panner;
}

async function createAmbientSpeaker(audioPath) {
    const source = audioCtx.createBufferSource();
    source.buffer = await loadAudio(audioPath);
    source.loop = true;
    source.connect(outdoorLowpassNode);
    return source;
}