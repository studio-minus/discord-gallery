function getDistanceFromWall(p) {
    const min = Math.min;
    const max = Math.max;

    let distance = 100000;

    let outerWall = sdCircle(p, 10);
    let innerWall = sdCircle(p, 9.5);
    distance = min(distance, (outerWall * innerWall));

    let cantGoAroundBack = sdBox(p, { x: 100, y: 12 }, { x: 0, y: -9 })
    distance = min(distance, max(cantGoAroundBack, -innerWall));

    let entrance = sdBox(p, { x: 0.8, y: 2 }, { x: 0, y: 9 })
    distance = max(distance, -entrance);

    let bear = sdBox(p, { x: 1.8, y: 1.5 }, { x: 7.3, y: 3.8 })
    distance = min(distance, bear);
    distance = min(distance, sdCircle(p, 2, { x: 7.5, y: 5.4 })); //extra littel blob

    let ezel1 = sdCircle(p, 1, { x: -6.8, y: -.02 })
    distance = min(distance, ezel1);

    let ezel2 = sdCircle(p, 1, { x: -4.73, y: -4.87 })
    distance = min(distance, ezel2);

    let ezel3 = sdCircle(p, 1, { x: 0, y: -6.81 })
    distance = min(distance, ezel3);

    let ezel4 = sdCircle(p, 1.1, { x: 4.85, y: -4.72 })
    distance = min(distance, ezel4);

    let ezel5 = sdCircle(p, 1.1, { x: 6.75, y: 0 })
    distance = min(distance, ezel5);

    distance = min(distance, sdCircle(p, 0.5, { x: 3.32, y: 0.012 }));
    distance = min(distance, sdCircle(p, 0.5, { x: -3.32, y: 0.012 }));

    //world bounds
    distance = min(distance, -sdCircle(p, 55));

    return distance;
}

function getNormalForWall(p) {
    const o = 0.2;
    let left = getDistanceFromWall({ x: p.x - o, y: p.y });
    let right = getDistanceFromWall({ x: p.x + o, y: p.y });
    let top = getDistanceFromWall({ x: p.x, y: p.y + o });
    let bottom = getDistanceFromWall({ x: p.x, y: p.y - o });

    let x = right - left;
    let y = top - bottom;
    let mg = Math.sqrt(x * x + y * y);
    x /= mg;
    y /= mg;

    return { x, y };
}

// https://iquilezles.org/articles/distfunctions2d/
// Thank you

function sdCircle(p, r, offset = { x: 0, y: 0 }) {
    const xx = p.x - offset.x;
    const yy = p.y - offset.y;
    return Math.sqrt(xx * xx + yy * yy) - r;
}

function sdBox(p, b, offset) {
    let d = {
        x: Math.abs(p.x - offset.x) - b.x,
        y: Math.abs(p.y - offset.y) - b.y
    }
    return Math.sqrt(Math.pow(Math.max(d.x, 0), 2) + Math.pow(Math.max(d.y, 0), 2)) + Math.min(Math.max(d.x, d.y), 0);
}