let audioCtx;//= new AudioContext();
let listener;
let masterGain;

let outdoorLowpassNode;
let outdoorGainNode;

let crowdSource;
let crowdGainNode;

let gramophoneSource;
let gramophoneLowpassNode;
let gramophoneAudioElem;

const genRange = (count) => Array.from({length: count}, (x, i) => i);

const pickRandom = (array) => array[Math.floor(Math.random() * array.length)];

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
    distantFriend: "sfx/distant.ogg",
    peopleTalking: "sfx/people-talking.ogg",
    wilJeNietHoren: "sfx/ditwiljenooithoren.mp3",
    birds: genRange(12).map(i => `sfx/birds/birds-${i}.ogg`),
    clicks: genRange(8).map(i => `sfx/geiger/click-0${i+1}.ogg`)
};

async function initaliseAudio() {
    audioCtx = new AudioContext();
    listener = audioCtx.listener;

    masterGain = new GainNode(audioCtx);
    masterGain.connect(audioCtx.destination);
    masterGain.gain.value = 1;
    

    gramophoneAudioElem = document.createElement('audio');

    outdoorLowpassNode = new BiquadFilterNode(audioCtx);
    outdoorLowpassNode.type = 'lowpass';
    outdoorLowpassNode.frequency.value = 1500;

    outdoorGainNode = new GainNode(audioCtx);
    outdoorGainNode.gain.value = 0.5;
    outdoorLowpassNode.connect(outdoorGainNode).connect(masterGain)

    let outdoorSound = await createAmbientSpeaker(sounds.outdoorAmbience);
    outdoorSound.connect(outdoorLowpassNode);
    outdoorSound.start();

    const lamps = await createPointSpeaker('sfx/lamp.wav', 0, 9, 0, 2, 30);
    lamps.loop = true;
    lamps.start();

    const lamp1 = await createPointSpeaker('sfx/lamp.wav', 5.8, 0, 16.6, 7, 8);
    lamp1.loop = true;
    lamp1.start();

    const lamp2 = await createPointSpeaker('sfx/lamp.wav', -5.8, 0, 16.6, 7, 8);
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
    const gramophoneSourcePanner = setAudioPosition(gramophoneSource, -4.53, 1.5, 5.422, 1, 25, 3);
    gramophoneSourcePanner.connect(gramophoneLowpassNode).connect(gramophoneReverb).connect(masterGain);
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

async function createPointSpeaker(audioPath, x, y, z, rolloffFactor = 2, maxDistance = 100, autoConnect = true, refDistance = 1) {
    const source = audioCtx.createBufferSource();
    source.buffer = await loadAudio(audioPath);
    const panner = setAudioPosition(source, x, y, z, rolloffFactor, maxDistance, refDistance);
    if (autoConnect)
        panner.connect(masterGain);

    return source;
}

function playOneShotPoint(audioPath, x, y, z, rolloffFactor = 2, maxDistance = 100, autoConnect = true, refDistance = 1){
    async function a() {
        const s = await createPointSpeaker(audioPath, x, y, z, rolloffFactor, maxDistance, autoConnect, refDistance);
        s.loop = false;
        s.start();
        s.addEventListener('ended', () => s.disconnect());
    }

    a().then(() => {});
}

function playOneShotAmbience(file, gain = 1){
    async function a() {
        const s = await createAmbientSpeaker(file);
        s.loop = false;
        const gainNode = new GainNode(audioCtx);
        gainNode.gain.value = gain;
        s.connect(gainNode).connect(masterGain);
        s.start();
        s.addEventListener('ended', () => s.disconnect());
    }

    a().then(() => {});
}

function setAudioPosition(source, x, y, z, rolloffFactor = 2, maxDistance = 100, refDistance = 1) {
    const panner = new PannerNode(audioCtx, {
        panningModel: 'HRTF',
        distanceModel: 'inverse',
        positionX: x,
        positionY: y,
        positionZ: z,
        refDistance,
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
    return source;
}
