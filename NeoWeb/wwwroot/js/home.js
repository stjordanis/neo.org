﻿var globe, globeCount = 0;
var width = 600, height = 600;

function createGlobe() {
    var newData = [];
    globeCount++;
    $("#globe canvas").remove();

    globe = new ENCOM.Globe(width, height, {
        font: "Inconsolata",
        data: newData, // copy the data array
        tiles: grid.tiles,
        baseColor: "#fd1b64",
        satelliteColor: "#ff0000",
        scale: 1.4,
        dayLength: 40 * 1000,
        introLinesColor: "#fd1b64",
        introLinesDuration: 1000
    });

    $("#globe").append(globe.domElement);
    globe.init(start);
}

function animate() {

    if (globe) {
        globe.tick();
    }

    lastTickTime = Date.now();

    requestAnimationFrame(animate);
}

function start() {
    if (globeCount == 1) { animate(); }
}

$(function () {

    if (!Detector.webgl) {
        Detector.addGetWebGLMessage({ parent: document.getElementById("container") });
        return;
    }

    WebFontConfig = {
        google: {
            families: ['Inconsolata']
        },
        active: function () {
            createGlobe();
        }
    };

    var wf = document.createElement('script');
    wf.src = ('https:' == document.location.protocol ? 'https' : 'http') +
        '://ajax.googleapis.com/ajax/libs/webfont/1.4.7/webfont.js';
    wf.type = 'text/javascript';
    wf.async = 'true';
    var s = document.getElementsByTagName('script')[0];
    s.parentNode.insertBefore(wf, s);

});
