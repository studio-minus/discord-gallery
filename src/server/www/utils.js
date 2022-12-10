function mapRange(a1, a2, b1, b2, s) {
    return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
}
function smoothApproach(pastPosition, pastTargetPosition, targetPosition, speed, deltaTime) {
    let t = deltaTime * speed;
    let v = (targetPosition - pastTargetPosition) / t;
    let f = pastPosition - pastTargetPosition + v;
    let result = targetPosition - v + f * Math.exp(-t);
    if (isNaN(result))
        return pastPosition;
    return result;
}

function randomRange(min, max){
    return mapRange(0, 1, min, max, Math.random());
}